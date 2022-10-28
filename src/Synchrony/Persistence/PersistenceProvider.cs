namespace Synchrony.Persistence;

public class PersistenceProvider :
    IPersistenceProvider
{
    public int GetStartOperation(Guid transactionId)
    {
        using var db = new TransactionDbContext();

        var operation = (from op in db.Operations
                where op.Id == transactionId && op.State != 1
                orderby op.SequenceNumber
                select op)
            .FirstOrDefault();

        if (operation is null)
            return 0;

        return operation.SequenceNumber - 1;
    }

    public bool TrySaveTransaction(Guid transactionId)
    {
        using var db = new TransactionDbContext();

        db.Transactions.Add(new TransactionEntity {Id = transactionId, State = (int)TransactionState.New, CreationTimestamp = DateTimeOffset.UtcNow});
        
        return true;
    }

    public bool TryUpdateTransaction(Guid transactionId, TransactionState state)
    {
        using var db = new TransactionDbContext();

        var transaction = (from trans in db.Transactions
                where trans.Id == transactionId
                select trans)
            .FirstOrDefault();

        if (transaction == null)
            return false;

        transaction.State = (int) state;

        db.Transactions.Update(transaction);

        return true;
    }

    public bool TrySaveOperation(TransactionOperation operation)
    {
        throw new NotImplementedException();
    }

    public bool TryUpdateOperationState(Guid operationId, OperationState state)
    {
        throw new NotImplementedException();
    }

    public IReadOnlyList<OperationEntity> GetAllOperations(Guid transactionId)
    {
        throw new NotImplementedException();
    }

    public bool TryGetTransaction(Guid transactionId, out TransactionEntity transaction)
    {
        throw new NotImplementedException();
    }

    public TransactionEntity TryGetTransaction(Guid transactionId)
    {
        throw new NotImplementedException();
    }
}