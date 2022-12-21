namespace Synchrony;

using Configuration;

public interface IOperation :
    IDisposable
{
    OperationMetadata Metadata { get; init; }
    
    OperationConfig Configure();

    Task<bool> Execute();

    Task<bool> Compensate();
}