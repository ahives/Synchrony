using Synchrony.Persistence;

namespace Synchrony.Extensions;

public static class TransactionExtensions
{
    public static bool VerifyIsExecutable(this TransactionOperation operation, Guid transactionId,
        IReadOnlyList<OperationEntity> operations, out ValidationResult result)
    {
        var op = operations.FirstOrDefault(x => x.Id == operation.OperationId);
        if (op is null)
        {
            result = new()
            {
                TransactionId = transactionId,
                OperationId = operation.OperationId,
                Type = ValidationType.Operation,
                Disposition = Disposition.Missing,
                Message = ""
            };
            
            return true;
        }
        
        switch ((OperationState)op.State)
        {
            case OperationState.Failed:
                result = new()
                {
                    TransactionId = op.TransactionId,
                    OperationId = op.Id,
                    Type = ValidationType.Operation,
                    Disposition = Disposition.Failed,
                    Message = ""
                };
                return true;
            case OperationState.New:
            case OperationState.Pending:
                result = new ValidationResult();
                return true;
            case OperationState.Completed:
            case OperationState.Compensated:
            default:
                result = new ValidationResult();
                return false;
        }
    }
}