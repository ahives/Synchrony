namespace Synchrony.Persistence;

using Microsoft.EntityFrameworkCore;

public class PersistenceProvider :
    IPersistenceProvider
{
    public int GetStartOperation(Guid transactionId)
    {
        using var db = new TransactionDbContext();

        var operations = (from op in db.Operations
                where op.Id == transactionId
                orderby op.SequenceNumber
                select op)
            .ToList();

        OperationEntity? operation = null;
        for (int i = 0; i < operations.Count; i++)
        {
            if (!IsOperationExecutable((TransactionStates) operations[i].State))
                continue;
            
            operation = operations[i];
            break;
        }

        if (operation is null)
            return 0;

        return operation.SequenceNumber == 0 ? 0 : operation.SequenceNumber - 1;
    }

    public Func<Guid, bool> TrySaveTransaction() =>
        transactionId =>
        {
            using var db = new TransactionDbContext();

            var entity = new TransactionEntity
            {
                Id = transactionId,
                State = (int) TransactionStates.New,
                CreationTimestamp = DateTimeOffset.UtcNow
            };

            db.Transactions.Add(entity);
            db.SaveChanges();

            return db.Entry(entity).State == EntityState.Added;
        };

    public Func<Guid, TransactionStates, bool> TryUpdateTransaction() =>
        (transactionId, state) =>
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
            db.SaveChanges();

            return db.Entry(transaction).State == EntityState.Modified;
        };

    // public Func<Guid, OperationState, bool> TryUpdateOperationState() =>
    //     (operationId, state) =>
    //     {
    //         using var db = new TransactionDbContext();
    //
    //         var operation = (from op in db.Operations
    //                 where op.Id == operationId
    //                 select op)
    //             .FirstOrDefault();
    //
    //         if (operation == null)
    //             return false;
    //
    //         operation.State = (int) state;
    //
    //         db.Operations.Update(operation);
    //         db.SaveChanges();
    //
    //         return db.Entry(operation).State == EntityState.Modified;
    //     };

    // public Func<TransactionOperation, bool> TrySaveOperation() =>
    //     operation =>
    //     {
    //         using var db = new TransactionDbContext();
    //
    //         var entity = new OperationEntity
    //         {
    //             Id = operation.OperationId,
    //             TransactionId = operation.TransactionId,
    //             Name = operation.Name,
    //             State = (int) TransactionStates.New,
    //             CreationTimestamp = DateTimeOffset.UtcNow
    //         };
    //
    //         db.Operations.Add(entity);
    //         db.SaveChanges();
    //
    //         return db.Entry(entity).State == EntityState.Added;
    //     };

    public IReadOnlyList<OperationEntity> GetAllOperations(Guid transactionId)
    {
        using var db = new TransactionDbContext();

        var operations = (from op in db.Operations
                where op.TransactionId == transactionId
                select op)
            .ToList();

        return operations;
    }

    public bool TryGetTransaction(Guid transactionId, out TransactionEntity transaction)
    {
        using var db = new TransactionDbContext();

        transaction = db.Transactions.Find(transactionId);

        return transaction is not null;
    }

    bool IsOperationExecutable(TransactionStates state) =>
        state switch
        {
            TransactionStates.New => true,
            TransactionStates.Pending => true,
            TransactionStates.Failed => true,
            _ => false
        };
}