namespace Synchrony;

public abstract class ObservableTransaction :
    IObservable<TransactionContext>
{
    protected readonly List<IObserver<TransactionContext>> _observers;

    protected ObservableTransaction()
    {
        _observers = new List<IObserver<TransactionContext>>();
    }

    public IDisposable Subscribe(IObserver<TransactionContext> observer)
    {
        if (!_observers.Contains(observer))
            _observers.Add(observer);

        return new UnSubscriber<TransactionContext>(_observers, observer);
    }

    protected virtual void StopSendingNotifications()
    {
        foreach (var observer in _observers)
            observer.OnCompleted();

        _observers.Clear();
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