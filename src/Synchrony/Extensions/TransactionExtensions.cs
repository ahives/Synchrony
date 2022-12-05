namespace Synchrony.Extensions;

using CommunityToolkit.Diagnostics;

internal static class TransactionExtensions
{
    internal static async Task<(bool success, int index)> ExecuteFrom(
        this List<IOperationBuilder> builders,
        int start,
        Func<IOperationBuilder, int, Task<bool>> function)
    {
        Guard.IsNotNull(builders);
        Guard.IsNotNull(function);

        bool succeed = true;
        for (int i = start; i < builders.Count; i++)
        {
            succeed &= await function(builders[i], i);
            if (!succeed)
                return (false, i);
        }

        return (true, -1);
    }

    internal static async Task<bool> CompensateFrom(
        this List<IOperationBuilder> builders,
        int start,
        Func<IOperationBuilder, Task<bool>> function)
    {
        Guard.IsNotNull(builders);
        Guard.IsNotNull(function);

        bool succeed = true;
        for (int i = start; i >= 0; i--)
        {
            succeed &= await function(builders[i]);
            if (!succeed)
                return false;
        }

        return true;
    }
}