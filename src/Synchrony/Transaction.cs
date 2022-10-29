namespace Synchrony;

using MassTransit;
using Configuration;
using Persistence;

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
            TransactionRetry = impl.TransactionRetry
        };

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
        ThrowIfSaveFailed(_persistence.TrySaveTransaction, GetTransactionId());

        var op = builder.Create(_transactionId, _operations.Count + 1);
        _operations.Add(op);

        ThrowIfSaveFailed(_persistence.TrySaveOperation, op);

        ThrowIfUpdateFailed(_persistence.TryUpdateTransaction, _transactionId, TransactionState.Pending);

        for (int i = 0; i < builders.Length; i++)
        {
            var operation = builders[i].Create(_transactionId, _operations.Count + 1);
            _operations.Add(operation);
            
            ThrowIfSaveFailed(_persistence.TrySaveOperation, operation);
        }

        return this;
    }

    public void Execute()
    {
        if (!IsTransactionExecutable(_transactionId))
            return;

        if (!TryDoWork(_transactionId, _operations, _config, out int index))
            return;

        bool compensated = TryDoCompensation(_transactionId, _operations, index, _config);
    }

    public static ITransaction Create()
    {
        return new Transaction(new PersistenceProvider());
    }

    public static ITransaction Create(IPersistenceProvider provider)
    {
        return new Transaction(provider);
    }


    class TransactionConfiguratorImpl :
        TransactionConfigurator
    {
        public bool LoggingOn { get; private set; }
        public bool ConsoleLoggingOn { get; private set; }
        public TransactionRetry TransactionRetry { get; private set; }


        public TransactionConfiguratorImpl()
        {
            TransactionRetry = TransactionRetry.None;
        }

        public void TurnOnLogging() => LoggingOn = true;

        public void TurnOnConsoleLogging() => ConsoleLoggingOn = true;
        
        public void Retry(TransactionRetry retry = TransactionRetry.None) => TransactionRetry = retry;
    }
}