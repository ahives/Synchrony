namespace Synchrony;

using Microsoft.Extensions.Logging;
using MassTransit;
using MassTransit.Mediator;
using StateMachines.Events;
using Configuration;
using Persistence;
using Extensions;

public sealed class Transaction :
    ObservableTransaction,
    ITransaction
{
    private readonly IPersistenceProvider _persistence;
    private readonly IReadOnlyList<OperationEntity> _operationsInDatabase;
    private readonly Guid _transactionId;
    private readonly IMediator _mediator;
    private readonly ILogger<Transaction> _logger;
    private readonly List<IOperationBuilder> _operations;
    private TransactionConfig _config;
    private bool _wasConfigured;

    public Transaction(IMediator mediator, ILogger<Transaction> logger)
    {
        _mediator = mediator;
        _logger = logger;
        _config = SynchronyConfigCache.Default;
        _operations = new List<IOperationBuilder>();
        _transactionId = NewId.NextGuid();
    }

    public ITransaction Configure(Action<TransactionConfigurator> configurator)
    {
        TransactionConfiguratorImpl impl = new TransactionConfiguratorImpl();
        configurator?.Invoke(impl);

        var config = new TransactionConfig
        {
            TransactionRetry = impl.TransactionRetry,
            Subscribers = impl.Subscribers
        };

        config.Subscribers.ForEach(x => Subscribe(x));
        
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
        
        _operations
            .ForEach(async builder =>
            {
                await _mediator.Publish<StartOperation>(new()
                {
                    OperationId = builder.GetId(),
                    TransactionId = _transactionId,
                    Name = builder.GetName(),
                    // SequenceNumber = operation.SequenceNumber
                });
            });

        (bool workSucceeded, int index) = await TryDoWork(_config);

        if (workSucceeded)
        {
            StopSendingNotifications();
            return;
        }

        bool compensated = await TryDoCompensation(index, _config);
        
        StopSendingNotifications();
    }

    async Task<(bool, int)> TryDoWork(TransactionConfig config)
    {
        // int start = _persistence.GetStartOperation(_transactionId);

        (bool succeeded, int index) =
            await _operations.ForEach(0, async (operation, _) =>
            {
                // _logger.LogInformation("Executing operation {OperationSequenceNumber}", operation.SequenceNumber);
                // Console.WriteLine($"Executing operation {operation.SequenceNumber}");

                bool success = operation.DoWork().Invoke();

                if (success)
                    await _mediator.Publish<OperationCompleted>(new()
                    {
                        OperationId = operation.GetId(),
                        TransactionId = _transactionId
                    });
                else
                    await _mediator.Publish<OperationFailed>(new()
                    {
                        OperationId = operation.GetId(),
                        TransactionId = _transactionId
                    });

                return success;
            });

        if (succeeded)
            await _mediator.Publish<TransactionCompleted>(new() {TransactionId = _transactionId});
        else
            await _mediator.Publish<TransactionFailed>(new() {TransactionId = _transactionId});

        return (succeeded, index);
    }

    async Task<bool> TryDoCompensation(
        int index,
        TransactionConfig config)
    {
        _operations.ForEach(index, async operation =>
        {
            // _logger.LogInformation("Compensating operation {OperationSequenceNumber}", operation.SequenceNumber);
            // Console.WriteLine($"Compensating operation {operation.SequenceNumber}");

            operation.OnFailure().Invoke();

            await _mediator.Publish<RequestCompensation>(new()
            {
                OperationId = operation.GetId(),
                TransactionId = _transactionId
            });
        });

        return true;
    }


    class TransactionConfiguratorImpl :
        TransactionConfigurator
    {
        private readonly List<IObserver<TransactionContext>> _subscribers;
        
        public bool LoggingOn { get; private set; }
        public TransactionRetry TransactionRetry { get; private set; }
        public List<IObserver<TransactionContext>> Subscribers => _subscribers;


        public TransactionConfiguratorImpl()
        {
            TransactionRetry = TransactionRetry.None;
            
            _subscribers = new List<IObserver<TransactionContext>>();
        }

        public void TurnOnLogging() => LoggingOn = true;
        
        public void Retry(TransactionRetry retry = TransactionRetry.None) => TransactionRetry = retry;

        public void Subscribe(object observer, params object[] observers)
        {
            Subscribe(observer);
            for (int i = 0; i < observers.Length; i++)
                Subscribe(observers[i]);
        }

        void Subscribe(object observer)
        {
            if (!observer.GetType().IsAssignableTo(typeof(IObserver<TransactionContext>)))
                return;

            if (observer is not IObserver<TransactionContext> op)
                return;
                
            _subscribers.Add(op);
        }
    }
}