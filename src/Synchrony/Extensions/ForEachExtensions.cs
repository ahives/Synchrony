namespace Synchrony.Extensions;

using CommunityToolkit.Diagnostics;
using Persistence;

internal static class ForEachExtensions
{
    internal static IEnumerable<TransactionOperation> ForEach(
        this List<IOperationBuilder> builders,
        IReadOnlyList<OperationEntity> operations,
        int start,
        Guid transactionId,
        Action<TransactionOperation> action)
    {
        Guard.IsNotNull(builders);
        Guard.IsNotNull(action);

        for (int i = 0; i < builders.Count; i++)
        {
            var operation = builders[i].Create(transactionId, start + i + 1);
            var targetOperation = operations.FirstOrDefault(x =>
                x.Name == operation.Name && (x.State == (int) OperationState.New ||
                                             x.State == (int) OperationState.Pending));
            if (targetOperation != default)
                continue;
            
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

    internal static (bool success, List<ValidationResult> results, int index) ForEach(
        this List<TransactionOperation> operations,
        IReadOnlyList<OperationEntity> persistedOperations,
        int start,
        Guid transactionId,
        Func<TransactionOperation, int, bool> function)
    {
        Guard.IsNotNull(operations);
        Guard.IsNotNull(function);

        bool succeed = true;
        var results = new List<ValidationResult>();
        for (int i = start; i < operations.Count; i++)
        {
            if (!operations[i].VerifyIsExecutable(transactionId, persistedOperations, out ValidationResult result))
            {
                results.Add(result);
                continue;
            }

            succeed &= function(operations[i], i);
            if (!succeed)
                return (succeed, results, i);
        }

        return (succeed, results, -1);
    }
}