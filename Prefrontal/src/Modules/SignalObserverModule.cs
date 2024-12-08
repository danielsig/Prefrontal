namespace Prefrontal.Modules;

internal class SignalObserverModule<TSignal>(List<IObserver<TSignal>> observers) : Module, ISignalReceiver<TSignal>, IDisposable
{
	internal List<IObserver<TSignal>> Observers = observers;
	public Task ReceiveSignalAsync(TSignal signal)
	{
		foreach(var observer in Observers)
			observer.OnNext(signal);
		return Task.CompletedTask;
	}
	public void Dispose()
	{
		foreach(var observer in Observers)
			observer.OnCompleted();
		Observers.Clear();
	}
}
