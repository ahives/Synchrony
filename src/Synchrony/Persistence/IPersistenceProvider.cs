namespace Synchrony.Persistence;

public interface IPersistenceProvider
{
    int GetStartOperation(Guid transactionId);

    bool TrySaveTransaction(Guid transactionId);

    bool TryUpdateTransaction(Guid transactionId, TransactionState state);
    
    bool TrySaveOperation(TransactionOperation operation);

    bool TryUpdateOperationState(Guid operationId, OperationState state);

    IReadOnlyList<OperationEntity> GetAllOperations(Guid transactionId);
    
    bool TryGetTransaction(Guid transactionId, out TransactionEntity transaction);
}