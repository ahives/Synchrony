namespace Synchrony;

internal class EmptyOperationBuilder :
    OperationBuilder<EmptyOperationBuilder>
{
    public override async Task<bool> Execute() => await Task.FromResult(true);

    public override async Task<bool> Compensate() => await Task.FromResult(true);
}