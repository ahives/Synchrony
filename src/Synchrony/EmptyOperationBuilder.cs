namespace Synchrony;

internal class EmptyOperationBuilder :
    OperationBuilder<EmptyOperationBuilder>
{
    public override Func<bool> DoWork() => () => false;

    public override Action OnFailure() => () => { };
}