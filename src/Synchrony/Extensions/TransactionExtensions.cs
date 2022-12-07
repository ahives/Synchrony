namespace Synchrony.Extensions;

using CommunityToolkit.Diagnostics;

internal static class TransactionExtensions
{
    internal static async Task<(bool success, int index)> ExecuteFrom(
        this List<IOperation> operations,
        int start,
        Func<IOperation, int, Task<bool>> function)
    {
        Guard.IsNotNull(operations);
        Guard.IsNotNull(function);

        bool succeed = true;
        for (int i = start; i < operations.Count; i++)
        {
            succeed &= await function(operations[i], i);
            if (!succeed)
                return (false, i);
        }

        return (true, -1);
    }

    internal static async Task<bool> CompensateFrom(
        this List<IOperation> operations,
        int start,
        Func<IOperation, Task<bool>> function)
    {
        Guard.IsNotNull(operations);
        Guard.IsNotNull(function);

        bool succeed = true;
        for (int i = start; i >= 0; i--)
        {
            succeed &= await function(operations[i]);
            if (!succeed)
                return false;
        }

        return true;
    }
}