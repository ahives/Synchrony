namespace Synchrony;

using Extensions;
using Configuration;
using Persistence;

public abstract class AtomicTransaction :
    ObservableTransaction
{
    protected readonly IPersistenceProvider _persistence;
    protected readonly IReadOnlyList<OperationEntity> _operationsInDatabase;
    protected readonly Guid _transactionId;

    protected AtomicTransaction(IPersistenceProvider persistence, Guid transactionId)
    {
        _persistence = persistence;
        _transactionId = transactionId;
        _operationsInDatabase = persistence.GetAllOperations(transactionId);
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

    protected virtual (bool succeeded, List<ValidationResult> results, int index) TryDoWork(
        List<TransactionOperation> operations,
        TransactionConfig config)
    {
        int start = _persistence.GetStartOperation(_transactionId);
        var persistedOperations = _persistence.GetAllOperations(_transactionId);

        (bool succeeded, List<ValidationResult> results, int index) =
            operations.ForEach(persistedOperations, start, _transactionId, (operation, _) =>
            {
                if (config.ConsoleLoggingOn)
                    Console.WriteLine($"Executing operation {operation.SequenceNumber}");

                _persistence
                    .TryUpdateOperationState()
                    .ThrowIfFailed(operation.OperationId, operation.TransactionId, OperationState.Pending,
                        _operationObservers);

                bool success = operation.Work.Invoke();

                _persistence
                    .TryUpdateOperationState()
                    .ThrowIfFailed(operation.OperationId, _transactionId,
                        success ? OperationState.Completed : OperationState.Failed, _operationObservers);

                return success;
            });

        _persistence
            .TryUpdateTransaction()
            .ThrowIfFailed(_transactionId, succeeded ? TransactionState.Completed : TransactionState.Failed, _transactionObservers);

        return (succeeded, results, index);
    }

    protected virtual bool TryDoCompensation(
        List<TransactionOperation> operations,
        Guid transactionId,
        int index,
        TransactionConfig config)
    {
        _persistence
            .TryUpdateTransaction()
            .ThrowIfFailed(transactionId, TransactionState.Failed, _transactionObservers);

        operations.ForEach(index, operation =>
        {
            if (config.ConsoleLoggingOn)
                Console.WriteLine($"Compensating operation {operation.SequenceNumber}");

            operation.Compensation.Invoke();

            _persistence
                .TryUpdateOperationState()
                .ThrowIfFailed(operation.OperationId, operation.TransactionId, OperationState.Compensated, _operationObservers);
        });

        _persistence
            .TryUpdateTransaction()
            .ThrowIfFailed(transactionId, TransactionState.Compensated, _transactionObservers);

        return true;
    }
}