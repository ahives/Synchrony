namespace Synchrony;

using MassTransit;
using Configuration;
using Persistence;
using Extensions;

public sealed class Transaction :
    AtomicTransaction,
    ITransaction
{
    private TransactionConfig _config;
    private readonly List<IOperationBuilder> _operationsInSession;
    private bool _wasConfigured;

    private Transaction(IPersistenceProvider persistence) : base(persistence, NewId.NextGuid())
    {
        _config = SynchronyConfigCache.Default;
        _operationsInSession = new List<IOperationBuilder>();
    }

    public ITransaction Configure(Action<TransactionConfigurator> configurator)
    {
        TransactionConfiguratorImpl impl = new TransactionConfiguratorImpl();
        configurator?.Invoke(impl);

        var config = new TransactionConfig
        {
            ConsoleLoggingOn = impl.ConsoleLoggingOn,
            TransactionRetry = impl.TransactionRetry,
        };

        impl.TransactionSubscribers.ForEach(x => Subscribe(x));
        impl.OperationSubscribers.ForEach(x => Subscribe(x));
        
        _config = config;

        _persistence
            .TrySaveTransaction()
            .ThrowIfFailed(GetTransactionId(), TransactionState.New, _transactionObservers);

        _wasConfigured = true;
        
        return this;
    }

    public ITransaction Configure()
    {
        return this;
    }

    public Guid GetTransactionId()
    {
        return _transactionId;
    }

    public ITransaction AddOperations(IOperationBuilder builder, params IOperationBuilder[] builders)
    {
        _operationsInSession.AddRange(builders.Prepend(builder).ToList());

        return this;
    }

    public void Execute()
    {
        if (!_wasConfigured)
            throw new SynchronyConfigurationException();
        
        if (!IsTransactionExecutable(_transactionId))
            return;

        _persistence
            .TryUpdateTransaction()
            .ThrowIfFailed(_transactionId, TransactionState.Pending, _transactionObservers);

        var operations = GetOperations();
        (bool workSucceeded, IReadOnlyList<ValidationResult> _, int index) = TryDoWork(operations, _config);

        if (workSucceeded)
        {
            StopSendingNotifications();
            return;
        }

        bool compensated = TryDoCompensation(operations, _transactionId, index, _config);
        
        StopSendingNotifications();
    }

    public static ITransaction Create() => Create(Database.Provider);

    public static ITransaction Create(IPersistenceProvider provider) => new Transaction(provider);

    List<TransactionOperation> GetOperations()
    {
        return _operationsInSession
            .ForEach(_operationsInDatabase, _operationsInDatabase.Count, _transactionId, (x, isNewRecord) =>
            {
                if (isNewRecord)
                    _persistence
                        .TrySaveOperation()
                        .ThrowIfFailed(x, OperationState.New, _operationObservers);
                else
                    _persistence
                        .TryUpdateOperationState()
                        .ThrowIfFailed(x.OperationId, x.TransactionId, OperationState.Pending, _operationObservers);
            })
            .ToList();
    }


    class TransactionConfiguratorImpl :
        TransactionConfigurator
    {
        private readonly List<IObserver<TransactionContext>> _transactionObservers;
        private readonly List<IObserver<OperationContext>> _operationObservers;
        
        public bool LoggingOn { get; private set; }
        public bool ConsoleLoggingOn { get; private set; }
        public TransactionRetry TransactionRetry { get; private set; }
        public List<IObserver<TransactionContext>> TransactionSubscribers => _transactionObservers;
        public List<IObserver<OperationContext>> OperationSubscribers => _operationObservers;


        public TransactionConfiguratorImpl()
        {
            TransactionRetry = TransactionRetry.None;
            
            _transactionObservers = new List<IObserver<TransactionContext>>();
            _operationObservers = new List<IObserver<OperationContext>>();
        }

        public void TurnOnLogging() => LoggingOn = true;

        public void TurnOnConsoleLogging() => ConsoleLoggingOn = true;
        
        public void Retry(TransactionRetry retry = TransactionRetry.None) => TransactionRetry = retry;

        public void Subscribe(object observer, params object[] observers)
        {
            Subscribe(observer);

            for (int i = 0; i < observers.Length; i++)
                Subscribe(observers[i]);
        }

        void Subscribe(object observer)
        {
            if (observer.GetType().IsAssignableTo(typeof(IObserver<TransactionContext>)))
            {
                if (observer is not IObserver<TransactionContext> op)
                    return;
                
                _transactionObservers.Add(op);
                return;
            }
            
            if (observer.GetType().IsAssignableTo(typeof(IObserver<OperationContext>)))
            {
                if (observer is not IObserver<OperationContext> op)
                    return;
                
                _operationObservers.Add(op);
            }
        }
    }
}