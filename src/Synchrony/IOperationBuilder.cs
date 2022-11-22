namespace Synchrony;

using Configuration;

public interface IOperationBuilder
{
    OperationConfig Configure();

    string GetName();

    Guid GetId();

    Func<bool> DoWork();

    Action DoOnFailure();
}