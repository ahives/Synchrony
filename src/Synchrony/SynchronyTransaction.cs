namespace Synchrony;

using CommunityToolkit.Diagnostics;
using Extensions;
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

    protected virtual bool TryDoWork(Guid transactionId, List<TransactionOperation> operations,
        TransactionConfig config, out IReadOnlyList<ValidationResult> results, out int index)
    {
        int start = _persistence.GetStartOperation(transactionId);
        var persistedOperations = _persistence.GetAllOperations(transactionId);
        var validationResults = new List<ValidationResult>();

        index = operations.ForEach(start, (x, _) =>
        {
            if (config.ConsoleLoggingOn)
                Console.WriteLine($"Executing operation {x.SequenceNumber}");

            bool success = false;
            if (!x.VerifyIsExecutable(transactionId, persistedOperations, out ValidationResult result))
            {
                validationResults.Add(result);
            }
            else
            {
                // TODO: if the current state is completed skip to the next operation

                ThrowIfUpdateFailed(_persistence.TryUpdateOperationState, x.OperationId, transactionId, OperationState.Pending);

                success = x.Work.Invoke();

                ThrowIfUpdateFailed(_persistence.TryUpdateOperationState, x.OperationId, transactionId,
                    success ? OperationState.Completed : OperationState.Failed);
            }

            return success;
        });

        results = validationResults;
        
        bool succeeded = index >= 0;

        ThrowIfUpdateFailed(_persistence.TryUpdateTransaction, transactionId,
            succeeded ? TransactionState.Completed : TransactionState.Failed);

        return succeeded;
    }

    protected virtual bool TryDoCompensation(Guid transactionId, List<TransactionOperation> operations,
        int index, TransactionConfig config)
    {
        ThrowIfUpdateFailed(_persistence.TryUpdateTransaction, transactionId, TransactionState.Failed);

        operations.ForEach(index, x =>
        {
            if (config.ConsoleLoggingOn)
                Console.WriteLine($"Compensating operation {x.SequenceNumber}");

            x.Compensation.Invoke();

            ThrowIfUpdateFailed(_persistence.TryUpdateOperationState, x.OperationId,
                x.TransactionId, OperationState.Compensated);
        });

        ThrowIfUpdateFailed(_persistence.TryUpdateTransaction, transactionId, TransactionState.Compensated);

        return true;
    }

    protected virtual void ThrowIfSaveFailed(Func<Guid, bool> save, Guid transactionId)
    {
        Guard.IsNotNull(save);

        if (!save.Invoke(transactionId))
            throw new TransactionPersistenceException();

        NotifyTransactionState(new() {TransactionId = transactionId, State = TransactionState.New});
    }

    protected virtual void ThrowIfSaveFailed(Func<TransactionOperation, bool> save, TransactionOperation operation)
    {
        Guard.IsNotNull(save);

        if (!save.Invoke(operation))
            throw new TransactionPersistenceException();

        NotifyOperationState(new()
        {
            TransactionId = operation.TransactionId, OperationId = operation.OperationId, State = OperationState.New
        });
    }

    protected virtual void ThrowIfUpdateFailed(Func<Guid, TransactionState, bool> update, Guid transactionId, TransactionState state)
    {
        Guard.IsNotNull(update);

        if (!update.Invoke(transactionId, state))
            throw new TransactionPersistenceException();

        NotifyTransactionState(new() {TransactionId = transactionId, State = state});
    }

    protected virtual void ThrowIfUpdateFailed(Func<Guid, OperationState, bool> update, Guid operationId,
        Guid transactionId, OperationState state)
    {
        Guard.IsNotNull(update);

        if (!update.Invoke(transactionId, state))
            throw new TransactionPersistenceException();

        NotifyOperationState(new() {OperationId = operationId, TransactionId = transactionId, State = state});
    }
}