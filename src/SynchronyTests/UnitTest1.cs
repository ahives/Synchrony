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
        OperationBuilder<Operation1>
    {
        public override Func<bool> DoWork()
        {
            return () => true;
        }

        public override Action OnFailure()
        {
            return () =>
            {
                Console.WriteLine("Something went wrong in Operation 1");
            };
        }
    }

    class Operation2 :
        OperationBuilder<Operation2>
    {
        public override Func<bool> DoWork()
        {
            return () => true;
        }

        public override Action OnFailure()
        {
            return () =>
            {
                Console.WriteLine("Something went wrong in Operation 2");
            };
        }
    }

    class Operation3 :
        OperationBuilder<Operation3>
    {
        public override Func<bool> DoWork()
        {
            return () => true;
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
            Console.WriteLine($"Transaction Observer: Transaction {value.TransactionId} is currently in state {value.State}");
        }
    }
}
