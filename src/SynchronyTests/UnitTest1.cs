using System.Diagnostics;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Synchrony.StateMachines;
using Synchrony.StateMachines.Sagas;

namespace SynchronyTests;

using Synchrony;
using Synchrony.Persistence;
using Synchrony.Testing;
using Microsoft.Extensions.DependencyInjection;

[TestFixture]
public class Tests
{
    private ServiceProvider _services;

    [OneTimeSetUp]
    public void Init()
    {
        // IConfiguration configuration = new ConfigurationBuilder()
        //     .AddJsonFile("appsettings.json", false)
        //     .Build();

        _services = new ServiceCollection()
            .AddSingleton<IPersistenceProvider, TestPersistenceProvider>()
            // .AddScoped(_ => configuration)
            .AddSingleton<ITransactionCache, TransactionCache>()
            .AddTransient<ITransaction, Transaction>()
            .AddMediator(x =>
            {
                // x.AddSagaRepository<TransactionState2>();
                x.AddSagaStateMachine<TransactionStateMachine, TransactionState>();
                x.AddSagaStateMachine<OperationStateMachine, OperationState>();
                x.SetInMemorySagaRepositoryProvider();
            })
            .AddLogging()
            // .AddDbContext<TransactionDbContext>(x =>
            //     x.UseNpgsql(configuration.GetConnectionString("")))
            .BuildServiceProvider();
    }
    
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public async Task Test1()
    {
        await _services.GetService<ITransaction>()
            ?.Configure(x =>
            {
                x.Subscribe(Observer.Create<MyObserver>());
            })
            .AddOperations(
                Operation.Create<Operation1>(),
                Operation.Create<Operation2>(),
                Operation.Create<Operation3>())
            .Execute()!;
        
        Assert.Pass();
    }

    class Operation1 :
        Operation<Operation1>
    {
        public override async Task<bool> Execute()
        {
            return await Task.FromResult(true);
        }

        public override async Task<bool> Compensate()
        {
            Console.WriteLine("Something went wrong in Operation 1");
            
            return await Task.FromResult(true);
        }
    }

    class Operation2 :
        Operation<Operation2>
    {
        public override async Task<bool> Execute()
        {
            return await Task.FromResult(true);
        }

        public override async Task<bool> Compensate()
        {
            Console.WriteLine("Something went wrong in Operation 2");
            
            return await Task.FromResult(true);
        }
    }

    class Operation3 :
        Operation<Operation3>
    {
        public override async Task<bool> Execute()
        {
            return await Task.FromResult(true);
        }
    }

    class MyObserver :
        IObserver<TransactionContext>
    {
        public void OnCompleted()
        {
            Console.WriteLine("MyObserver1 completed");
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(TransactionContext value)
        {
            Console.WriteLine($"Transaction Observer: Transaction {value.OperationId} is currently in state {value.State}");
        }
    }
}
