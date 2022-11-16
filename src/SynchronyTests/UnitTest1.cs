using MassTransit;
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
        _services = new ServiceCollection()
            .AddSingleton<IPersistenceProvider, TestPersistenceProvider>()
            .AddTransient<ITransaction, Transaction>()
            .AddMediator(x =>
            {
                // x.AddSagaRepository<TransactionState2>();
                x.AddSagaStateMachine<TransactionStateMachine, TransactionState>();
                x.AddSagaStateMachine<OperationStateMachine, OperationState>();
                x.SetInMemorySagaRepositoryProvider();
            })
            .BuildServiceProvider();
    }
    
    [SetUp]
    public void Setup()
    {
    }

    // [Test]
    // public void Test1()
    // {
    //     var op1 = Operation.Create<Operation1>();
    //     var op2 = Operation.Create<Operation2>();
    //     var op3 = Operation.Create<Operation3>();
    //
    //     new Transaction(TestDatabase.Provider)
    //         .Configure(x =>
    //         {
    //             x.TurnOnConsoleLogging();
    //             x.Retry();
    //         })
    //         .AddOperations(op1, op2, op3)
    //         .Execute();
    //     
    //     Assert.Pass();
    // }

    [Test]
    public async Task Test2()
    {
        var op1 = Operation.Create<Operation1>();
        var op2 = Operation.Create<Operation2>();
        var op3 = Operation.Create<Operation3>();

        await _services.GetService<ITransaction>()
            ?.Configure(x =>
            {
                // x.TurnOnConsoleLogging();
                // x.Retry();
                // x.Subscribe(Observer.Create<MyObserver2>(), Observer.Create<MyObserver>());
                x.Subscribe(Observer.Create<MyObserver>());
            })
            .AddOperations(op1, op2, op3)
            .Execute()!;
        
        Assert.Pass();
    }

    [Test]
    public void Test3()
    {
        var op1 = Operation.Create<Operation1>();
        var op2 = Operation.Create<Operation2>();
        var op3 = Operation.Create<Operation3>();

        _services.GetService<ITransaction>()
            .Configure(x =>
            {
                x.TurnOnConsoleLogging();
                x.Retry();
            })
            .AddOperations(op1, op2, op3)
            .Execute();
        
        Assert.Pass();
    }

    class Operation1 :
        OperationBuilder<Operation1>
    {
        protected override Func<bool> DoWork()
        {
            return () => true;
        }

        protected override Action Compensate()
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
        protected override Func<bool> DoWork()
        {
            return () => true;
        }

        protected override Action Compensate()
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
        protected override Func<bool> DoWork()
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
