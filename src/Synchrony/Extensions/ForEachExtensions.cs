namespace Synchrony.Extensions;

using CommunityToolkit.Diagnostics;

internal static class ForEachExtensions
{
    internal static IEnumerable<TransactionOperation> ForEach(
        this List<IOperationBuilder> builders,
        int start,
        Guid transactionId,
        Action<TransactionOperation> action)
    {
        Guard.IsNotNull(builders);
        Guard.IsNotNull(action);

        for (int i = 0; i < builders.Count; i++)
        {
            var operation = builders[i].Create(transactionId, start + i + 1);

            action(operation);
            
            yield return operation;
        }
    }

    internal static void ForEach<T>(this List<T> list, int start, Action<T> action)
    {
        Guard.IsNotNull(list);
        Guard.IsNotNull(action);

        for (int i = start; i >= 0; i--)
            action(list[i]);
    }

    internal static async Task<(bool success, int index)> ForEach(
        this List<TransactionOperation> operations,
        int start,
        Func<TransactionOperation, int, Task<bool>> function)
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