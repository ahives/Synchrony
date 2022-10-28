namespace Synchrony;

using Configuration;

public class TransactionOperation
{
    public string Name { get; init; }
    
    public Guid OperationId { get; init; }
    
    public Guid TransactionId { get; init; }
    
    public int SequenceNumber { get; init; }

    public OperationConfig Config { get; set; }

    public Func<bool> Work { get; set; }

    public Action Compensation { get; set; }
}