namespace Synchrony.Extensions;

using CommunityToolkit.Diagnostics;

internal static class ForEachExtensions
{
    internal static void ForEach<T>(this List<T> list, int start, Action<T> action)
    {
        Guard.IsNotNull(list);
        Guard.IsNotNull(action);

        for (int i = start; i >= 0; i--)
            action(list[i]);
    }

    internal static async Task<(bool success, int index)> ForEach(
        this List<IOperationBuilder> operations,
        int start,
        Func<IOperationBuilder, int, Task<bool>> function)
    {
        Guard.IsNotNull(operations);
        Guard.IsNotNull(function);

        bool succeed = true;
        for (int i = start; i < operations.Count; i++)
        {
            succeed &= await function(operations[i], i);
            if (!succeed)
                return (succeed, i);
        }

        return (succeed, -1);
    }
}