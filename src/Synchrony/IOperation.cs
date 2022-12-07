namespace Synchrony;

using Configuration;

public interface IOperation
{
    OperationConfig Configure();

    string GetName();

    Guid GetId();

    Task<bool> Execute();

    Task<bool> Compensate();
}