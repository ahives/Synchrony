namespace Synchrony;

using MassTransit.Mediator;
using StateMachines.Events;
using Extensions;
using Configuration;
using Persistence;

public abstract class AtomicTransaction :
    ObservableTransaction
{
    protected readonly IPersistenceProvider _persistence;
    protected readonly IReadOnlyList<OperationEntity> _operationsInDatabase;
    protected readonly Guid _transactionId;
    protected readonly IMediator _mediator;

    protected AtomicTransaction(Guid transactionId, IMediator mediator)
    {
        _transactionId = transactionId;
        _mediator = mediator;
    }

    protected virtual async Task<(bool, int)> TryDoWork(List<TransactionOperation> operations, TransactionConfig config)
    {
        // int start = _persistence.GetStartOperation(_transactionId);

        (bool succeeded, int index) =
            await operations.ForEach(0, async (operation, _) =>
            {
                if (config.ConsoleLoggingOn)
                    Console.WriteLine($"Executing operation {operation.SequenceNumber}");

                bool success = operation.Work.Invoke();

                if (success)
                    await _mediator.Publish<OperationCompleted>(new()
                    {
                        OperationId = operation.OperationId,
                        TransactionId = _transactionId
                    });
                else
                    await _mediator.Publish<OperationFailed>(new()
                    {
                        OperationId = operation.OperationId,
                        TransactionId = _transactionId
                    });

                return success;
            });

        if (succeeded)
            await _mediator.Publish<TransactionCompleted>(new() {TransactionId = _transactionId});
        else
            await _mediator.Publish<TransactionFailed>(new() {TransactionId = _transactionId});

        return (succeeded, index);
    }

    protected virtual async Task<bool> TryDoCompensation(
        List<TransactionOperation> operations,
        Guid transactionId,
        int index,
        TransactionConfig config)
    {
        operations.ForEach(index, async operation =>
        {
            if (config.ConsoleLoggingOn)
                Console.WriteLine($"Compensating operation {operation.SequenceNumber}");

            operation.Compensation.Invoke();

            await _mediator.Publish<RequestCompensation>(new() {OperationId = operation.OperationId, TransactionId = transactionId});
        });

        return true;
    }
}