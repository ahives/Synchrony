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

    public Func<Guid, TransactionState, bool> TryUpdateTransaction()
    {
        return (_, _) => true;
    }

    public Func<Guid, OperationState, bool> TryUpdateOperationState()
    {
        return (_, _) => true;
    }

    public Func<TransactionOperation, bool> TrySaveOperation()
    {
        return _ => true;
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

    public Func<Guid, bool> TrySaveTransaction()
    {
        return _ => true;
    }
}