namespace Synchrony;

using MassTransit;
using MassTransit.Mediator;
using StateMachines.Events;
using Configuration;
using Persistence;
using Extensions;

public sealed class Transaction :
    AtomicTransaction,
    ITransaction
{
    private TransactionConfig _config;
    private readonly List<IOperationBuilder> _operations;
    private bool _wasConfigured;

    public Transaction(IMediator mediator) : base(NewId.NextGuid(), mediator)
    {
        _config = SynchronyConfigCache.Default;
        _operations = new List<IOperationBuilder>();
    }

    public ITransaction Configure(Action<TransactionConfigurator> configurator)
    {
        TransactionConfiguratorImpl impl = new TransactionConfiguratorImpl();
        configurator?.Invoke(impl);

        var config = new TransactionConfig
        {
            ConsoleLoggingOn = impl.ConsoleLoggingOn,
            TransactionRetry = impl.TransactionRetry,
        };

        impl.TransactionSubscribers.ForEach(x => Subscribe(x));
        impl.OperationSubscribers.ForEach(x => Subscribe(x));
        
        _config = config;

        _wasConfigured = true;
        
        return this;
    }

    public ITransaction Configure()
    {
        return this;
    }

    public Guid GetTransactionId()
    {
        return _transactionId;
    }

    public ITransaction AddOperations(IOperationBuilder builder, params IOperationBuilder[] builders)
    {
        _operations.AddRange(builders.Prepend(builder).ToList());

        return this;
    }

    public async Task Execute()
    {
        if (!_wasConfigured)
            throw new SynchronyConfigurationException();

        await _mediator.Publish<StartTransaction>(new() {TransactionId = GetTransactionId()});

        var operations = GetOperations();
        (bool workSucceeded, int index) = await TryDoWork(operations, _config);

        if (workSucceeded)
        {
            StopSendingNotifications();
            return;
        }

        bool compensated = await TryDoCompensation(operations, _transactionId, index, _config);
        
        StopSendingNotifications();
    }

    List<TransactionOperation> GetOperations()
    {
        return _operations
            .ForEach(0, _transactionId, async operation =>
                {
                    await _mediator.Publish<StartOperation>(new()
                    {
                        OperationId = operation.OperationId,
                        TransactionId = _transactionId,
                        Name = operation.Name,
                        SequenceNumber = operation.SequenceNumber
                    });
                })
            .ToList();
    }


    class TransactionConfiguratorImpl :
        TransactionConfigurator
    {
        private readonly List<IObserver<TransactionContext>> _transactionObservers;
        private readonly List<IObserver<OperationContext>> _operationObservers;
        
        public bool LoggingOn { get; private set; }
        public bool ConsoleLoggingOn { get; private set; }
        public TransactionRetry TransactionRetry { get; private set; }
        public List<IObserver<TransactionContext>> TransactionSubscribers => _transactionObservers;
        public List<IObserver<OperationContext>> OperationSubscribers => _operationObservers;


        public TransactionConfiguratorImpl()
        {
            TransactionRetry = TransactionRetry.None;
            
            _transactionObservers = new List<IObserver<TransactionContext>>();
            _operationObservers = new List<IObserver<OperationContext>>();
        }

        public void TurnOnLogging() => LoggingOn = true;

        public void TurnOnConsoleLogging() => ConsoleLoggingOn = true;
        
        public void Retry(TransactionRetry retry = TransactionRetry.None) => TransactionRetry = retry;

        public void Subscribe(object observer, params object[] observers)
        {
            Subscribe(observer);

            for (int i = 0; i < observers.Length; i++)
                Subscribe(observers[i]);
        }

        void Subscribe(object observer)
        {
            if (observer.GetType().IsAssignableTo(typeof(IObserver<TransactionContext>)))
            {
                if (observer is not IObserver<TransactionContext> op)
                    return;
                
                _transactionObservers.Add(op);
                return;
            }
            
            if (observer.GetType().IsAssignableTo(typeof(IObserver<OperationContext>)))
            {
                if (observer is not IObserver<OperationContext> op)
                    return;
                
                _operationObservers.Add(op);
            }
        }
    }
}