namespace Synchrony;

internal class EmptyOperationBuilder :
    OperationBuilder<EmptyOperationBuilder>
{
    public override async Task<bool> DoWork() => await Task.FromResult(true);

    public override async Task<bool> DoCompensation() => await Task.FromResult(true);
}