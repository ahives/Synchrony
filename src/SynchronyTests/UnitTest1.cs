using Synchrony;
using Synchrony.Persistence;
using Synchrony.Testing;

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
            .BuildServiceProvider();
    }
    
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Test1()
    {
        var op1 = Operation.Create<Operation1>();
        var op2 = Operation.Create<Operation2>();
        var op3 = Operation.Create<Operation3>();

        new Transaction(TestPersistence.Provider)
            .Configure(x =>
            {
                x.TurnOnConsoleLogging();
                x.Retry();
            })
            .AddOperations(op1, op2, op3)
            .Execute();
        
        Assert.Pass();
    }

    [Test]
    public void Test2()
    {
        var op1 = Operation.Create<Operation1>();
        var op2 = Operation.Create<Operation2>();
        var op3 = Operation.Create<Operation3>();

        Transaction.Create(TestPersistence.Provider)
            .Configure(x =>
            {
                x.TurnOnConsoleLogging();
                x.Retry();
            })
            .AddOperations(op1, op2, op3)
            .Execute();
        
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
            return () => false;
        }
    }
}