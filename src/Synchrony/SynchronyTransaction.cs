namespace Synchrony;

using Configuration;
using Persistence;

public abstract class SynchronyTransaction :
    ObservableTransaction
{
    private readonly IPersistenceProvider _persistence;

    protected SynchronyTransaction(IPersistenceProvider persistence)
    {
        _persistence = persistence;
    }

    protected virtual bool IsTransactionExecutable(Guid transactionId)
    {
        if (!_persistence.TryGetTransaction(transactionId, out TransactionEntity transaction))
            return true;

        TransactionState state = (TransactionState) transaction.State;

        return state switch
        {
            TransactionState.Completed => false,
            _ => true
        };
    }

    protected virtual bool TryDoWork(Guid transactionId, IReadOnlyList<TransactionOperation> operations,
        TransactionConfig config, out IReadOnlyList<ValidationResult> results, out int index)
    {
        bool failed = false;
        int start = _persistence.GetStartOperation(transactionId);

        index = -1;
        
        var persistedOperations = _persistence.GetAllOperations(transactionId);
        var validationResults = new List<ValidationResult>();

        for (int i = start; i < operations.Count; i++)
        {
            if (config.ConsoleLoggingOn)
                Console.WriteLine($"Executing operation {operations[i].SequenceNumber}");

            if (!operations[i].VerifyIsExecutable(transactionId, persistedOperations, out ValidationResult result))
            {
                validationResults.Add(result);
                continue;
            }
            
            // TODO: if the current state is completed skip to the next operation
            
            ThrowIfUpdateFailed(_persistence.TryUpdateOperationState, operations[i].OperationId, transactionId, OperationState.Pending);

            if (operations[i].Work.Invoke())
            {
                ThrowIfUpdateFailed(_persistence.TryUpdateOperationState, operations[i].OperationId, transactionId, OperationState.Completed);
                continue;
            }

            ThrowIfUpdateFailed(_persistence.TryUpdateOperationState, operations[i].OperationId, transactionId, OperationState.Failed);

            failed = true;
            index = i;
            break;
        }

        results = validationResults;

        ThrowIfUpdateFailed(_persistence.TryUpdateTransaction, transactionId,
            failed ? TransactionState.Failed : TransactionState.Completed);

        return failed;
    }

    protected virtual bool TryDoCompensation(Guid transactionId, IReadOnlyList<TransactionOperation> operations,
        int index, TransactionConfig config)
    {
        ThrowIfUpdateFailed(_persistence.TryUpdateTransaction, transactionId, TransactionState.Failed);

        for (int i = index; i >= 0; i--)
        {
            if (config.ConsoleLoggingOn)
                Console.WriteLine($"Compensating operation {operations[i].SequenceNumber}");

            operations[i].Compensation.Invoke();

            ThrowIfUpdateFailed(_persistence.TryUpdateOperationState, operations[i].OperationId,
                operations[i].TransactionId, OperationState.Compensated);
        }

        ThrowIfUpdateFailed(_persistence.TryUpdateTransaction, transactionId, TransactionState.Compensated);

        return true;
    }

    protected virtual void ThrowIfSaveFailed(Func<Guid, bool> save, Guid transactionId)
    {
        if (save is null)
            throw new ArgumentNullException();

        if (!save.Invoke(transactionId))
            throw new TransactionPersistenceException();

        NotifyTransactionState(new() {TransactionId = transactionId, State = TransactionState.New});
    }

    protected virtual void ThrowIfSaveFailed(Func<TransactionOperation, bool> save, TransactionOperation operation)
    {
        if (save is null)
            throw new ArgumentNullException();

        if (!save.Invoke(operation))
            throw new TransactionPersistenceException();

        NotifyOperationState(new()
        {
            TransactionId = operation.TransactionId, OperationId = operation.OperationId, State = OperationState.New
        });
    }

    protected virtual void ThrowIfUpdateFailed(Func<Guid, TransactionState, bool> update, Guid transactionId, TransactionState state)
    {
        if (update is null)
            throw new ArgumentNullException();

        if (!update.Invoke(transactionId, state))
            throw new TransactionPersistenceException();

        NotifyTransactionState(new() {TransactionId = transactionId, State = state});
    }

    protected virtual void ThrowIfUpdateFailed(Func<Guid, OperationState, bool> update, Guid operationId,
        Guid transactionId, OperationState state)
    {
        if (update is null)
            throw new ArgumentNullException();

        if (!update.Invoke(transactionId, state))
            throw new TransactionPersistenceException();

        NotifyOperationState(new() {OperationId = operationId, TransactionId = transactionId, State = state});
    }
}