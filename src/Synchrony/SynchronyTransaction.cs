namespace Synchrony;

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

        index =
            operations.ForEach(start, (x, _) =>
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

                    _persistence
                        .TryUpdateOperationState()
                        .ThrowIfFailed(x.OperationId, x.TransactionId, OperationState.Pending, _operationObservers);

                    success = x.Work.Invoke();

                    _persistence
                        .TryUpdateOperationState()
                        .ThrowIfFailed(x.OperationId, transactionId,
                            success ? OperationState.Completed : OperationState.Failed, _operationObservers);
                }

                return success;
            });

        results = validationResults;
        
        bool succeeded = index >= 0;

        _persistence
            .TryUpdateTransaction()
            .ThrowIfFailed(transactionId, succeeded ? TransactionState.Completed : TransactionState.Failed, _transactionObservers);

        return succeeded;
    }

    protected virtual bool TryDoCompensation(Guid transactionId, List<TransactionOperation> operations,
        int index, TransactionConfig config)
    {
        _persistence
            .TryUpdateTransaction()
            .ThrowIfFailed(transactionId, TransactionState.Failed, _transactionObservers);

        operations.ForEach(index, x =>
        {
            if (config.ConsoleLoggingOn)
                Console.WriteLine($"Compensating operation {x.SequenceNumber}");

            x.Compensation.Invoke();

            _persistence
                .TryUpdateOperationState()
                .ThrowIfFailed(x.OperationId, x.TransactionId, OperationState.Compensated, _operationObservers);
        });

        _persistence
            .TryUpdateTransaction()
            .ThrowIfFailed(transactionId, TransactionState.Compensated, _transactionObservers);

        return true;
    }
}