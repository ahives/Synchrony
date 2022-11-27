namespace Synchrony;

using Configuration;

public interface IOperationBuilder
{
    OperationConfig Configure();

    string GetName();

    Guid GetId();

    Task<bool> DoWork();

    Task<bool> DoCompensation();
}