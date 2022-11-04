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

    internal static (bool success, int index) ForEach<T>(this List<T> list, int start, Func<T, int, bool> function)
    {
        Guard.IsNotNull(list);
        Guard.IsNotNull(function);

        bool succeed = true;
        for (int i = start; i < list.Count; i++)
        {
            succeed &= function(list[i], i);
            if (!succeed)
                return (succeed, i);
        }

        return (succeed, -1);
    }
}