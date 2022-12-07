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
    private readonly List<IOperation> _operations;
    private TransactionConfig _config;
    private bool _wasConfigured;

    public Transaction(IMediator mediator, ITransactionCache cache, ILogger<Transaction> logger)
    {
        _mediator = mediator;
        _cache = cache;
        _logger = logger;
        _config = SynchronyConfigCache.Default;
        _operations = new List<IOperation>();
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

    public ITransaction AddOperations(IOperation operation, params IOperation[] operations)
    {
        _operations.AddRange(operations.Prepend(operation).ToList());

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
                int start = 0;
                
                await _mediator
                    .Publish<StartTransaction>(new() {TransactionId = GetTransactionId()}, cancellationToken)
                    .ContinueWith(async x =>
                    {
                        (bool succeeded, int index) =
                            await _operations.ExecuteFrom(start,
                                async (operation, _) => await TryExecute(operation, _config, cancellationToken));

                        if (succeeded)
                        {
                            StopSendingNotifications();
                            return;
                        }

                        bool compensated = await _operations.CompensateFrom(index,
                            async operation => await TryCompensate(operation, _config, cancellationToken));

                        StopSendingNotifications();
                    }, cancellationToken)
                    .Unwrap();
            }, cancellationToken)
            .Unwrap();
    }

    async Task<bool> TryCompensate(IOperation operation, TransactionConfig config, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Compensating operation {Name}", operation.GetName());
        // Console.WriteLine($"Compensating operation {operation.SequenceNumber}");

        return await _mediator
            .Publish<RequestCompensation>(new()
            {
                OperationId = operation.GetId(),
                TransactionId = _transactionId
            }, cancellationToken)
            .ContinueWith(async _ =>
            {
                try
                {
                    var compensationTask = Task.Run(operation.Compensate, cancellationToken);

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

    async Task<bool> TryExecute(IOperation operation, TransactionConfig config, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Executing operation {Name}", operation.GetName());
        // Console.WriteLine($"Executing operation {operation.SequenceNumber}");

        return await _mediator
            .Publish<RequestExecuteOperation>(new()
            {
                OperationId = operation.GetId(),
                TransactionId = _transactionId,
                Name = operation.GetName(),
            }, cancellationToken)
            .ContinueWith(async _ =>
            {
                try
                {
                    var workTask = Task.Run(operation.Execute, cancellationToken);

                    return await workTask
                        .ContinueWith(async task =>
                        {
                            if (task.Result)
                            {
                                await _mediator.Publish<OperationCompleted>(new()
                                    {
                                        OperationId = operation.GetId(),
                                        TransactionId = _transactionId
                                    }, cancellationToken);
                            }
                            else
                            {
                                await _mediator.Publish<OperationFailed>(new()
                                    {
                                        OperationId = operation.GetId(),
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
                            OperationId = operation.GetId(),
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

        public void Subscribe(IObserver<TransactionContext> observer, params IObserver<TransactionContext>[] observers)
        {
            _subscribers.Add(observer);
            for (int i = 0; i < observers.Length; i++)
                _subscribers.Add(observers[i]);
        }
    }
}