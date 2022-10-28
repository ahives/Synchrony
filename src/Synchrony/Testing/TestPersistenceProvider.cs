namespace Synchrony.Testing;

using MassTransit;
using Persistence;

public class TestPersistenceProvider :
    IPersistenceProvider
{
    public int GetStartOperation(Guid transactionId)
    {
        return 0;
    }

    public bool TrySaveTransaction(Guid transactionId)
    {
        return true;
    }

    public bool TryUpdateTransaction(Guid transactionId, TransactionState state)
    {
        return true;
    }

    public bool TrySaveOperation(TransactionOperation operation)
    {
        return true;
    }

    public bool TryUpdateOperationState(Guid operationId, OperationState state)
    {
        return true;
    }

    public IReadOnlyList<OperationEntity> GetAllOperations(Guid transactionId)
    {
        var operations = new List<OperationEntity>();

        operations.Add(new OperationEntity()
        {
            Id = NewId.NextGuid(), Name = "test-1", TransactionId = transactionId, SequenceNumber = 1, State = 1,
            CreationTimestamp = DateTimeOffset.UtcNow
        });
        operations.Add(new OperationEntity()
        {
            Id = NewId.NextGuid(), Name = "test-2", TransactionId = transactionId, SequenceNumber = 2, State = 1,
            CreationTimestamp = DateTimeOffset.UtcNow
        });
        operations.Add(new OperationEntity()
        {
            Id = NewId.NextGuid(), Name = "test-3", TransactionId = transactionId, SequenceNumber = 3, State = 1,
            CreationTimestamp = DateTimeOffset.UtcNow
        });

        return operations;
    }

    public bool TryGetTransaction(Guid transactionId, out TransactionEntity transaction)
    {
        transaction = new TransactionEntity
        {
            Id = NewId.NextGuid(), State = (int) TransactionState.Pending, CreationTimestamp = DateTimeOffset.UtcNow
        };
        return true;
    }
}