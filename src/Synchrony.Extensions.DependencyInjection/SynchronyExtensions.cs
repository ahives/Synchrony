namespace Synchrony.Extensions.DependencyInjection;

using Microsoft.Extensions.DependencyInjection;
using Persistence;

public static class SynchronyExtensions
{
    public static IServiceCollection AddSynchrony(this IServiceCollection services)
    {
        services.AddSingleton<IPersistenceProvider, PersistenceProvider>();
        services.AddTransient<ITransaction, Transaction>();

        return services;
    }
}