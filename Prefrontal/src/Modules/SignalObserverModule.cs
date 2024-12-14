namespace Prefrontal.Modules;

internal class SignalObserverModule<TSignal>(List<IObserver<TSignal>> observers) : Module, IAsyncSignalReceiver<TSignal>, IDisposable
{
	internal List<IObserver<TSignal>> Observers = observers;
	public Task ReceiveSignalAsync(TSignal signal)
	{
		foreach(var observer in new List<IObserver<TSignal>>(Observers))
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
		var observers = Observers;
		Observers = [];
		foreach(var observer in observers)
			try
			{
				observer.OnCompleted();
			}
			catch(Exception ex)
			{
				Debug.LogError(ex, "An error occurred while notifying an observer that the signal stream has completed.");
			}
		observers.Clear();
	}
}
