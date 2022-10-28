using Synchrony;
using Synchrony.Persistence;

namespace Atomicity.Extensions.DependencyInjection;

using Microsoft.Extensions.DependencyInjection;

public static class AtomicityExtensions
{
    public static IServiceCollection AddAtomicity(this IServiceCollection services)
    {
        services.AddSingleton<IPersistenceProvider, PersistenceProvider>();
        services.AddTransient<ITransaction, Transaction>();

        return services;
    }
}