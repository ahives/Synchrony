namespace Synchrony;

using MassTransit;
using Configuration;
using Persistence;

public class Transaction :
    ITransaction
{
    private TransactionConfig _config;
    private readonly IPersistenceProvider _persistence;
    private readonly List<TransactionOperation> _operations;
    private readonly Guid _transactionId;

    public Transaction(IPersistenceProvider persistence)
    {
        _config = SynchronyConfigCache.Default;
        _persistence = persistence;
        _operations = new List<TransactionOperation>();
        _transactionId = NewId.NextGuid();
    }

    public Transaction Configure(Action<TransactionConfigurator> configurator)
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

    public Transaction Configure()
    {
        return this;
    }

    public Transaction AddOperations(IOperationBuilder builder, params IOperationBuilder[] builders)
    {
        ThrowIfSaveFailed(_persistence.TrySaveTransaction, _transactionId);

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
        // if (!IsVerified(out ValidationResult report))
        //     return;

        if (!TryDoWork(_transactionId, _operations, out int index))
            return;

        bool compensated = TryDoCompensation(_transactionId, _operations, index);
    }

    public static Transaction Create()
    {
        return new Transaction(new PersistenceProvider());
    }

    public static Transaction Create(IPersistenceProvider provider)
    {
        return new Transaction(provider);
    }

    bool IsVerified(out ValidationResult report)
    {
        if (!_persistence.TryGetTransaction(_transactionId, out TransactionEntity transaction))
        {
            report = new() {TransactionId = _transactionId, Message = "Could not find transaction in the database."};
            return false;
        }
        
        var operations = _persistence.GetAllOperations(_transactionId);

        if (operations.Count != _operations.Count)
        {
            report = new(){TransactionId = _transactionId, Message = ""};
            return true;
        }

        List<ValidationResult> results = new List<ValidationResult>();
        for (int i = 0; i < operations.Count; i++)
        {
            if (_operations.All(x => x.OperationId != operations[i].Id))
            {
                results.Add(new()
                {
                    TransactionId = _transactionId,
                    OperationId = operations[i].Id,
                    Disposition = Disposition.Missing,
                    Message = ""
                });
                continue;
            }
        }
        
        // TODO: add logic to compare database and in-memory operations before executing
        report = new ValidationResult();
        return true;
    }

    // public IReadOnlyList<OperationStatus> GetOperationStatus()
    // {
    //     return _persistence
    //         .GetAllOperations(_transactionId)
    //         .Select(x => new {x.TransactionId, OperationId = x.Id, (OperationState)x.State});
    // }

    bool TryDoWork(Guid transactionId, IReadOnlyList<TransactionOperation> operations, out int index)
    {
        bool operationFailed = false;
        int start = _persistence.GetStartOperation(transactionId);

        index = -1;

        ThrowIfUpdateFailed(_persistence.TryUpdateTransaction, _transactionId, TransactionState.Pending);
        
        var persistedOperations = _persistence.GetAllOperations(_transactionId);
        var results = new List<ValidationResult>();

        for (int i = start; i < _operations.Count; i++)
        {
            if (_config.ConsoleLoggingOn)
                Console.WriteLine($"Executing operation {operations[i].SequenceNumber}");

            if (!operations[i].VerifyIsExecutable(_transactionId, persistedOperations, out ValidationResult result))
            {
                results.Add(result);
                continue;
            }
            
            // TODO: if the current state is completed skip to the next operation
            
            ThrowIfUpdateFailed(_persistence.TryUpdateOperationState, transactionId, OperationState.Pending);

            if (operations[i].Work.Invoke())
            {
                ThrowIfUpdateFailed(_persistence.TryUpdateOperationState, transactionId, OperationState.Completed);
                continue;
            }

            ThrowIfUpdateFailed(_persistence.TryUpdateOperationState, transactionId, OperationState.Failed);

            operationFailed = true;
            index = i;
            break;
        }

        if (operationFailed)
            ThrowIfUpdateFailed(_persistence.TryUpdateTransaction, _transactionId, TransactionState.Failed);

        return operationFailed;
    }

    bool TryDoCompensation(Guid transactionId, IReadOnlyList<TransactionOperation> operations, int index)
    {
        ThrowIfUpdateFailed(_persistence.TryUpdateTransaction, transactionId, TransactionState.Failed);

        for (int i = index; i >= 0; i--)
        {
            if (_config.ConsoleLoggingOn)
                Console.WriteLine($"Compensating operation {operations[i].SequenceNumber}");

            operations[i].Compensation.Invoke();

            ThrowIfUpdateFailed(_persistence.TryUpdateOperationState, operations[i].OperationId, OperationState.Compensated);
        }

        ThrowIfUpdateFailed(_persistence.TryUpdateTransaction, transactionId, TransactionState.Compensated);

        return true;
    }

    void ThrowIfSaveFailed(Func<Guid, bool> save, Guid transactionId)
    {
        if (save is null)
            throw new ArgumentNullException();
        
        if (save.Invoke(transactionId))
            return;

        throw new TransactionPersistenceException();
    }

    void ThrowIfSaveFailed(Func<TransactionOperation, bool> save, TransactionOperation operation)
    {
        if (save is null)
            throw new ArgumentNullException();
        
        if (save.Invoke(operation))
            return;

        throw new TransactionPersistenceException();
    }

    void ThrowIfUpdateFailed(Func<Guid, TransactionState, bool> update, Guid transactionId, TransactionState state)
    {
        if (update is null)
            throw new ArgumentNullException();
        
        if (update.Invoke(transactionId, state))
            return;

        throw new TransactionPersistenceException();
    }

    void ThrowIfUpdateFailed(Func<Guid, OperationState, bool> update, Guid transactionId, OperationState state)
    {
        if (update is null)
            throw new ArgumentNullException();
        
        if (update.Invoke(transactionId, state))
            return;

        throw new TransactionPersistenceException();
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