namespace Synchrony;

public abstract class ObservableTransaction :
    IObservable<TransactionContext>,
    IObservable<OperationContext>
{
    private readonly List<IObserver<TransactionContext>> _transactionObservers;
    private readonly List<IObserver<OperationContext>> _operationObservers;

    protected ObservableTransaction()
    {
        _transactionObservers = new List<IObserver<TransactionContext>>();
        _operationObservers = new List<IObserver<OperationContext>>();
    }

    public IDisposable Subscribe(IObserver<TransactionContext> observer)
    {
        if (!_transactionObservers.Contains(observer))
            _transactionObservers.Add(observer);

        return new UnSubscriber<TransactionContext>(_transactionObservers, observer);
    }

    public IDisposable Subscribe(IObserver<OperationContext> observer)
    {
        if (!_operationObservers.Contains(observer))
            _operationObservers.Add(observer);

        return new UnSubscriber<OperationContext>(_operationObservers, observer);
    }

    protected virtual void NotifyTransactionState(TransactionContext context)
    {
        foreach (var observer in _transactionObservers)
        {
            switch (context.State)
            {
                case TransactionState.New:
                case TransactionState.Pending:
                case TransactionState.Completed:
                case TransactionState.Compensated:
                    observer.OnNext(context);
                    break;
                case TransactionState.Failed:
                default:
                    observer.OnError(new TransactionPersistenceException());
                    break;
            }
        }
    }

    protected virtual void NotifyOperationState(OperationContext context)
    {
        foreach (var observer in _operationObservers)
        {
            switch (context.State)
            {
                case OperationState.New:
                case OperationState.Pending:
                case OperationState.Completed:
                case OperationState.Compensated:
                    observer.OnNext(context);
                    break;
                case OperationState.Failed:
                default:
                    observer.OnError(new TransactionPersistenceException());
                    break;
            }
        }
    }

    protected virtual void StopNotifying()
    {
        foreach (var observer in _transactionObservers)
            observer.OnCompleted();

        _transactionObservers.Clear();

        foreach (var observer in _operationObservers)
            observer.OnCompleted();

        _operationObservers.Clear();
    }

    
    class UnSubscriber<T> :
        IDisposable
    {
        private readonly List<IObserver<T>> _observers;
        private readonly IObserver<T> _observer;

        public UnSubscriber(List<IObserver<T>> observers, IObserver<T> observer)
        {
            _observers = observers;
            _observer = observer;
        }

        public void Dispose()
        {
            if (_observers.Contains(_observer))
                _observers.Remove(_observer);
        }
    }
}