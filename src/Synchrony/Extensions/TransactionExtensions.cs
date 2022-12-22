namespace Synchrony.Extensions;

using CommunityToolkit.Diagnostics;

internal static class TransactionExtensions
{
    internal static async Task<(bool success, int index)> ExecuteFrom(
        this List<IOperation> operations,
        int start,
        Func<IOperation, int, Task<bool>> execute)
    {
        Guard.IsNotNull(operations);
        Guard.IsNotNull(execute);

        bool succeed = true;
        for (int i = start; i < operations.Count; i++)
        {
            succeed &= await execute(operations[i], i);
            if (!succeed)
                return (false, i);
        }

        return (true, -1);
    }

    internal static async Task<bool> CompensateFrom(
        this List<IOperation> operations,
        int start,
        Func<IOperation, Task<bool>> compensate)
    {
        Guard.IsNotNull(operations);
        Guard.IsNotNull(compensate);

        bool succeed = true;
        for (int i = start; i >= 0; i--)
        {
            succeed &= await compensate(operations[i]);
            if (!succeed)
                return false;
        }

        return true;
    }

    internal static async Task<bool> TryRun(this IOperation operation, Func<IOperation, Task<bool>> run, Func<Task<bool>> action)
    {
        Guard.IsNotNull(operation);
        Guard.IsNotNull(run);
        Guard.IsNotNull(action);

        try
        {
            return await run(operation);
        }
        catch
        {
            return await action();
        }
        finally
        {
            operation.Dispose();
        }
    }
}