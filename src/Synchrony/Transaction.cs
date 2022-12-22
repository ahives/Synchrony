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
    private Guid _transactionId;
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

    public IEnumerable<IObserver<TransactionContext>> GetSubscribers() => _subscribers;

    public ITransaction AddOperations(IOperation operation, params IOperation[] operations)
    {
        _operations.AddRange(operations.Prepend(operation).ToList());

        return this;
    }

    public async Task Execute(Guid transactionId, CancellationToken cancellationToken = default) =>
        await ExecuteTransaction(transactionId, cancellationToken);

    public async Task Execute(CancellationToken cancellationToken = default) =>
        await ExecuteTransaction(NewId.NextGuid(), cancellationToken);

    async Task ExecuteTransaction(Guid transactionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        if (!_wasConfigured)
            throw new SynchronyConfigurationException();

        _transactionId = transactionId;
        
        _cache.Store(this);
        
        int start = 0;
                
        await _mediator.Publish<StartTransaction>(new() {TransactionId = transactionId}, cancellationToken);

        (bool succeeded, int index) =
            await _operations.ExecuteFrom(start,
                async (operation, _) => await TryExecute(transactionId, operation, _config, cancellationToken));

        if (succeeded)
        {
            StopSendingNotifications();
            return;
        }

        bool compensated = await _operations.CompensateFrom(index,
                    async operation => await TryCompensate(transactionId, operation, _config, cancellationToken));

        StopSendingNotifications();
    }

    async Task<bool> TryCompensate(Guid transactionId, IOperation operation, TransactionConfig config, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Compensating operation {Name}", operation.Metadata.Name);
        // Console.WriteLine($"Compensating operation {operation.SequenceNumber}");

        await _mediator
            .Publish<RequestCompensation>(new()
            {
                OperationId = operation.Metadata.Id,
                TransactionId = transactionId
            }, cancellationToken);

        return await operation.TryRun(o => o.Compensate(), () => Task.FromResult(false));
    }

    async Task<bool> TryExecute(Guid transactionId, IOperation operation, TransactionConfig config, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Executing operation {Name}", operation.Metadata.Name);
        // Console.WriteLine($"Executing operation {operation.SequenceNumber}");

        await _mediator
            .Publish<RequestExecuteOperation>(new()
            {
                OperationId = operation.Metadata.Id,
                TransactionId = transactionId,
                Name = operation.Metadata.Name,
            }, cancellationToken);

        return await operation.TryRun(
            o => ExecuteOperation(o, transactionId, _mediator, cancellationToken),
            async () =>
            {
                await _mediator.Publish<OperationFailed>(new()
                {
                    OperationId = operation.Metadata.Id,
                    TransactionId = transactionId,
                }, cancellationToken);

                return false;
            });
    }

    async Task<bool> ExecuteOperation(IOperation operation, Guid transactionId, IMediator mediator, CancellationToken cancellationToken)
    {
        var succeeded = await operation.Execute();

        if (succeeded)
        {
            await mediator.Publish<OperationCompleted>(new()
            {
                OperationId = operation.Metadata.Id,
                TransactionId = transactionId
            }, cancellationToken);
        }
        else
        {
            await mediator.Publish<OperationFailed>(new()
            {
                OperationId = operation.Metadata.Id,
                TransactionId = transactionId,
            }, cancellationToken);
        }

        return succeeded;
    }


    class TransactionConfiguratorImpl :
        TransactionConfigurator
    {
        private readonly List<IObserver<TransactionContext>> _subscribers;
        
        public TransactionRetry TransactionRetry { get; private set; }
        public List<IObserver<TransactionContext>> Subscribers => _subscribers;


        public TransactionConfiguratorImpl()
        {
            TransactionRetry = TransactionRetry.None;
            
            _subscribers = new List<IObserver<TransactionContext>>();
        }
        
        public void Retry(TransactionRetry retry = TransactionRetry.None) => TransactionRetry = retry;

        public void Subscribe(IObserver<TransactionContext> subscriber, params IObserver<TransactionContext>[] subscribers)
        {
            _subscribers.Add(subscriber);
            for (int i = 0; i < subscribers.Length; i++)
                _subscribers.Add(subscribers[i]);
        }
    }
}