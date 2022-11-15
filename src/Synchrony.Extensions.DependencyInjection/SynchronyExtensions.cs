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
            x.AddSagaStateMachine<TransactionStateMachine, TransactionState2>();
            x.AddSagaStateMachine<OperationStateMachine, OperationState2>();
            x.SetInMemorySagaRepositoryProvider();
        });

        return services;
    }
}