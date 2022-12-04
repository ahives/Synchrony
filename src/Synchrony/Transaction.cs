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
    private readonly ITransactionCache _cache;
    private readonly ILogger<Transaction> _logger;
    private readonly List<IOperationBuilder> _operations;
    private TransactionConfig _config;
    private bool _wasConfigured;

    public Transaction(IMediator mediator, ITransactionCache cache, ILogger<Transaction> logger)
    {
        _mediator = mediator;
        _cache = cache;
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

    public IReadOnlyList<IObserver<TransactionContext>> GetObservers() => _observers;

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

        await Task.Run(() => _cache.Store(this), cancellationToken)
            .ContinueWith(async _ =>
            {
                await _mediator
                    .Publish<StartTransaction>(new() {TransactionId = GetTransactionId()}, cancellationToken)
                    .ContinueWith(async x =>
                    {
                        (bool workSucceeded, int index) =
                            await _operations.ForEach(0,
                                async (builder, _) => await TryDoWork(builder, _config, cancellationToken));

                        if (workSucceeded)
                        {
                            StopSendingNotifications();
                            return;
                        }

                        bool compensated = await _operations.ForEach(index,
                            async builder => await TryDoCompensation(builder, _config, cancellationToken));

                        StopSendingNotifications();
                    }, cancellationToken)
                    .Unwrap();
            }, cancellationToken)
            .Unwrap();
    }

    async Task<bool> TryDoCompensation(IOperationBuilder builder, TransactionConfig config, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Compensating operation {Name}", builder.GetName());
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
                                _mediator.Publish<OperationCompleted>(new()
                                    {
                                        OperationId = builder.GetId(),
                                        TransactionId = _transactionId
                                    }, cancellationToken)
                                    .GetAwaiter()
                                    .GetResult();
                            }
                            else
                            {
                                _mediator.Publish<OperationFailed>(new()
                                    {
                                        OperationId = builder.GetId(),
                                        TransactionId = _transactionId,
                                    }, cancellationToken)
                                    .GetAwaiter()
                                    .GetResult();
                            }

                            return task.Result;
                        }, cancellationToken)
                        .Unwrap();
                }
                catch
                {
                    _mediator.Publish<OperationFailed>(new()
                        {
                            OperationId = builder.GetId(),
                            TransactionId = _transactionId,
                        }, cancellationToken)
                        .GetAwaiter()
                        .GetResult();

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

        public void Subscribe(IObserver<TransactionContext> observer, params IObserver<TransactionContext>[] observers)
        {
            _subscribers.Add(observer);
            for (int i = 0; i < observers.Length; i++)
                _subscribers.Add(observers[i]);
        }
    }
}