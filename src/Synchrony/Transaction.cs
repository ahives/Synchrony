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

    public async Task Execute(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        if (!_wasConfigured)
            throw new SynchronyConfigurationException();

        await _mediator.Publish<StartTransaction>(new() {TransactionId = GetTransactionId()}, cancellationToken);
        
        (bool workSucceeded, int index) =
            await _operations.ForEach(0, async (builder, _) => await TryDoWork(builder, _config, cancellationToken));

        if (workSucceeded)
        {
            StopSendingNotifications();
            return;
        }

        bool compensated = await _operations.ForEach(index,
            async builder => await TryDoCompensation(builder, _config, cancellationToken));

        StopSendingNotifications();
    }

    async Task<bool> TryDoCompensation(IOperationBuilder builder, TransactionConfig config, CancellationToken cancellationToken)
    {
        // _logger.LogInformation("Compensating operation {OperationSequenceNumber}", builder.SequenceNumber);
        // Console.WriteLine($"Compensating operation {builder.SequenceNumber}");

        return await _mediator
            .Publish<RequestCompensation>(new()
            {
                OperationId = builder.GetId(),
                TransactionId = _transactionId
            }, cancellationToken)
            .ContinueWith(async _ =>
            {
                try
                {
                    var compensationTask = Task.Run(builder.DoCompensation, cancellationToken);

                    return await compensationTask
                        .ContinueWith(async task => task.Result, cancellationToken)
                        .Unwrap();
                }
                catch
                {
                    return false;
                }
            }, cancellationToken)
            .Unwrap();
    }

    async Task<bool> TryDoWork(IOperationBuilder builder, TransactionConfig config, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Executing operation {Name}", builder.GetName());
        // Console.WriteLine($"Executing operation {builder.SequenceNumber}");

        return await _mediator
            .Publish<RequestExecuteOperation>(new()
            {
                OperationId = builder.GetId(),
                TransactionId = _transactionId,
                Name = builder.GetName(),
            }, cancellationToken)
            .ContinueWith(async _ =>
            {
                try
                {
                    var workTask = Task.Run(builder.DoWork, cancellationToken);

                    return await workTask
                        .ContinueWith(async task =>
                        {
                            if (task.Result)
                            {
                                await _mediator.Publish<OperationCompleted>(new()
                                {
                                    OperationId = builder.GetId(),
                                    TransactionId = _transactionId
                                }, cancellationToken);
                            }
                            else
                            {
                                await _mediator.Publish<OperationFailed>(new()
                                {
                                    OperationId = builder.GetId(),
                                    TransactionId = _transactionId,
                                }, cancellationToken);
                            }

                            return task.Result;
                        }, cancellationToken)
                        .Unwrap();
                }
                catch
                {
                    await _mediator.Publish<OperationFailed>(new()
                    {
                        OperationId = builder.GetId(),
                        TransactionId = _transactionId,
                    }, cancellationToken);

                    return false;
                }
            }, cancellationToken)
            .Unwrap();
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