namespace Synchrony;

using MassTransit;
using Configuration;
using Persistence;
using Extensions;

public sealed class Transaction :
    SynchronyTransaction,
    ITransaction
{
    private TransactionConfig _config;
    private readonly IPersistenceProvider _persistence;
    private readonly List<TransactionOperation> _operations;
    private readonly Guid _transactionId;

    public Transaction(IPersistenceProvider persistence) : base(persistence)
    {
        _config = SynchronyConfigCache.Default;
        _persistence = persistence;
        _operations = new List<TransactionOperation>();
        _transactionId = NewId.NextGuid();
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
        _persistence
            .TrySaveTransaction()
            .ThrowIfFailed(GetTransactionId(), TransactionState.New, _transactionObservers);

        var op = builder.Create(_transactionId, _operations.Count + 1);
        _operations.Add(op);

        _persistence
            .TrySaveOperation()
            .ThrowIfFailed(op, OperationState.New, _operationObservers);

        _persistence
            .TryUpdateTransaction()
            .ThrowIfFailed(_transactionId, TransactionState.Pending, _transactionObservers);

        for (int i = 0; i < builders.Length; i++)
        {
            var operation = builders[i].Create(_transactionId, _operations.Count + 1);
            _operations.Add(operation);

            _persistence
                .TrySaveOperation()
                .ThrowIfFailed(operation, OperationState.New, _operationObservers);
        }

        return this;
    }

    public void Execute()
    {
        if (!IsTransactionExecutable(_transactionId))
            return;

        if (TryDoWork(_transactionId, _operations, _config, out IReadOnlyList<ValidationResult> results, out int index))
        {
            StopSendingNotifications();
            return;
        }

        bool compensated = TryDoCompensation(_transactionId, _operations, index, _config);
        
        StopSendingNotifications();
    }

    public static ITransaction Create() => new Transaction(Database.Provider);

    public static ITransaction Create(IPersistenceProvider provider) => new Transaction(provider);


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