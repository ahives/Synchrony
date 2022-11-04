namespace Synchrony.Persistence;

public interface IPersistenceProvider
{
    int GetStartOperation(Guid transactionId);

    Func<Guid, TransactionState, bool> TryUpdateTransaction();

    Func<Guid, OperationState, bool> TryUpdateOperationState();
    
    Func<TransactionOperation, bool> TrySaveOperation();

    Func<Guid, bool> TrySaveTransaction();

    IReadOnlyList<OperationEntity> GetAllOperations(Guid transactionId);
    
    bool TryGetTransaction(Guid transactionId, out TransactionEntity transaction);
}