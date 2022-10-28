namespace Synchrony;

public record ValidationResult
{
    public Guid TransactionId { get; init; }

    public Guid OperationId { get; init; }

    public string Message { get; init; }

    public ValidationType Type { get; init; }

    public Disposition Disposition { get; init; }
}

public enum ValidationType
{
    Transaction,
    Operation
}

public enum Disposition
{
    Failed,
    Missing
}