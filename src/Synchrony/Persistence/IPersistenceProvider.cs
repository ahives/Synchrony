namespace Synchrony.Persistence;

public interface IPersistenceProvider
{
    int GetStartOperation(Guid transactionId);

    Func<Guid, TransactionStates, bool> TryUpdateTransaction();
    
    Func<TransactionOperation, bool> TrySaveOperation();

    Func<Guid, bool> TrySaveTransaction();

    IReadOnlyList<OperationEntity> GetAllOperations(Guid transactionId);
    
    bool TryGetTransaction(Guid transactionId, out TransactionEntity transaction);
}