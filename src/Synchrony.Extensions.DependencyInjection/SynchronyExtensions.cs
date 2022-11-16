namespace Synchrony.Extensions.DependencyInjection;

using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using StateMachines;
using StateMachines.Sagas;
using Persistence;

public static class SynchronyExtensions
{
    public static IServiceCollection AddSynchrony(this IServiceCollection services)
    {
        services.AddSingleton<IPersistenceProvider, PersistenceProvider>();
        services.AddTransient<ITransaction, Transaction>();
        services.AddMediator(x =>
        {
            x.AddSagaStateMachine<TransactionStateMachine, TransactionState>();
            x.AddSagaStateMachine<OperationStateMachine, OperationState>();
            x.SetInMemorySagaRepositoryProvider();
        });

        return services;
    }
}