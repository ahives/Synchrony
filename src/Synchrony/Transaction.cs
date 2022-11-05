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
    private readonly IPersistenceProvider _persistence;
    private readonly IReadOnlyList<OperationEntity> _operationsInDatabase;
    private readonly List<IOperationBuilder> _operationsInSession;
    private readonly Guid _transactionId;

    private Transaction(IPersistenceProvider persistence, Guid transactionId) : base(persistence)
    {
        _config = SynchronyConfigCache.Default;
        _persistence = persistence;
        _operationsInDatabase = persistence.GetAllOperations(transactionId);
        _operationsInSession = new List<IOperationBuilder>();
        _transactionId = transactionId;
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
        if (!IsTransactionExecutable(_transactionId))
            return;

        _persistence
            .TryUpdateTransaction()
            .ThrowIfFailed(_transactionId, TransactionState.Pending, _transactionObservers);

        var operations = GetOperations();
        (bool workSucceeded, IReadOnlyList<ValidationResult> _, int index) = TryDoWork(operations, _transactionId, _config);

        if (workSucceeded)
        {
            StopSendingNotifications();
            return;
        }

        bool compensated = TryDoCompensation(operations, _transactionId, index, _config);
        
        StopSendingNotifications();
    }

    public static ITransaction Create() => Create(Database.Provider);

    public static ITransaction Create(IPersistenceProvider provider)
    {
        var transactionId = NewId.NextGuid();
        
        return new Transaction(provider, transactionId);
    }

    List<TransactionOperation> GetOperations()
    {
        return _operationsInSession
            .ForEach(_operationsInDatabase, _operationsInDatabase.Count, _transactionId, x =>
            {
                _persistence
                    .TrySaveOperation()
                    .ThrowIfFailed(x, OperationState.New, _operationObservers);
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