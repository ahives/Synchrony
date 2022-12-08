namespace Synchrony;

public record OperationMetadata
{
    public string Name { get; init; }

    public Guid Id { get; init; }
}