namespace Prefrontal.Modules;

internal class SignalObserverModule<TSignal>() : Module, IAsyncSignalReceiver<TSignal>, IDisposable
{
	private readonly Lock _gate = new();
	private IObserver<TSignal>[] _observers = [];
	internal void AddObserver(IObserver<TSignal> observer)
	{
		lock(_gate)
			_observers = [.. _observers, observer];
	}
	internal void RemoveObserver(IObserver<TSignal> observer)
	{
		lock(_gate)
			_observers = _observers.Except(observer).ToArray();
	}
	public Task ReceiveSignalAsync(TSignal signal)
	{
		IObserver<TSignal>[] observers;
		lock(_gate)
			observers = _observers;
		foreach(var observer in observers)
			try
			{
				observer.OnNext(signal);
			}
			catch(Exception ex)
			{
				Debug.LogError(ex, "An error occurred while notifying an observer of a signal.");
			}
		return Task.CompletedTask;
	}
	public void Dispose()
	{
		var observers = _observers;
		_observers = [];
		foreach(var observer in observers)
			try
			{
				observer.OnCompleted();
			}
			catch(Exception ex)
			{
				Debug.LogError(ex, "An error occurred while notifying an observer that the signal stream has completed.");
			}
	}
}
