namespace Synchrony;

using Configuration;
using Persistence;

public class SynchronyTransaction
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

    protected virtual bool TryDoWork(Guid transactionId, IReadOnlyList<TransactionOperation> operations, TransactionConfig config, out int index)
    {
        bool operationFailed = false;
        int start = _persistence.GetStartOperation(transactionId);

        index = -1;

        ThrowIfUpdateFailed(_persistence.TryUpdateTransaction, transactionId, TransactionState.Pending);
        
        var persistedOperations = _persistence.GetAllOperations(transactionId);
        var results = new List<ValidationResult>();

        for (int i = start; i < operations.Count; i++)
        {
            if (config.ConsoleLoggingOn)
                Console.WriteLine($"Executing operation {operations[i].SequenceNumber}");

            if (!operations[i].VerifyIsExecutable(transactionId, persistedOperations, out ValidationResult result))
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
            ThrowIfUpdateFailed(_persistence.TryUpdateTransaction, transactionId, TransactionState.Failed);

        return operationFailed;
    }

    protected virtual bool TryDoCompensation(Guid transactionId, IReadOnlyList<TransactionOperation> operations, int index, TransactionConfig config)
    {
        ThrowIfUpdateFailed(_persistence.TryUpdateTransaction, transactionId, TransactionState.Failed);

        for (int i = index; i >= 0; i--)
        {
            if (config.ConsoleLoggingOn)
                Console.WriteLine($"Compensating operation {operations[i].SequenceNumber}");

            operations[i].Compensation.Invoke();

            ThrowIfUpdateFailed(_persistence.TryUpdateOperationState, operations[i].OperationId, OperationState.Compensated);
        }

        ThrowIfUpdateFailed(_persistence.TryUpdateTransaction, transactionId, TransactionState.Compensated);

        return true;
    }

    protected virtual void ThrowIfSaveFailed(Func<Guid, bool> save, Guid transactionId)
    {
        if (save is null)
            throw new ArgumentNullException();
        
        if (save.Invoke(transactionId))
            return;

        throw new TransactionPersistenceException();
    }

    protected virtual void ThrowIfSaveFailed(Func<TransactionOperation, bool> save, TransactionOperation operation)
    {
        if (save is null)
            throw new ArgumentNullException();
        
        if (save.Invoke(operation))
            return;

        throw new TransactionPersistenceException();
    }

    protected virtual void ThrowIfUpdateFailed(Func<Guid, TransactionState, bool> update, Guid transactionId, TransactionState state)
    {
        if (update is null)
            throw new ArgumentNullException();
        
        if (update.Invoke(transactionId, state))
            return;

        throw new TransactionPersistenceException();
    }

    protected virtual void ThrowIfUpdateFailed(Func<Guid, OperationState, bool> update, Guid transactionId, OperationState state)
    {
        if (update is null)
            throw new ArgumentNullException();
        
        if (update.Invoke(transactionId, state))
            return;

        throw new TransactionPersistenceException();
    }
}