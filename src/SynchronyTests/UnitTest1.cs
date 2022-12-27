namespace SynchronyTests;

using MassTransit;
using Synchrony.StateMachines;
using Synchrony.StateMachines.Sagas;
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
            ?.Configure(x => { x.Subscribe(Subscriber.Create<MyObserver>()); })
            .AddOperations(
                Operation.Create<Operation1>(new TestDependency()),
                Operation.Create<Operation2>(),
                Operation.Create<Operation3>())
            .Execute()!;

        Assert.Pass();
    }

    [Test]
    public async Task Test2()
    {
        ITransaction transaction = _services.GetService<ITransaction>();
        await transaction
            ?.Configure(x => { x.Subscribe(Subscriber.Create<MyObserver>()); })
            .AddOperations(
                Operation.Create<Operation1>(new TestDependency()),
                Operation.Create<Operation2>(),
                Operation.Create<Operation3>())
            .Execute()!;

        Console.WriteLine(transaction.Metadata.Hash);
        Assert.Pass();
    }

    class Operation1 :
        Operation<Operation1>
    {
        private readonly ITestDependency _dependency;

        public Operation1(ITestDependency dependency)
        {
            _dependency = dependency;
        }

        public override async Task<bool> Execute()
        {
            _dependency.DoSomething();
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
            return await Task.FromResult(false);
        }
    }

    class MyObserver :
        IObserver<Synchrony.TransactionContext>
    {
        public void OnCompleted()
        {
            Console.WriteLine("MyObserver1 completed");
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(Synchrony.TransactionContext value)
        {
            Console.WriteLine(
                $"Transaction Observer: Transaction {value.OperationId} is currently in state {value.State}");
        }
    }

    interface ITestDependency
    {
        void DoSomething();
    }

    class TestDependency :
        ITestDependency
    {
        public void DoSomething()
        {
            Console.WriteLine("Some dependency");
        }
    }
}
