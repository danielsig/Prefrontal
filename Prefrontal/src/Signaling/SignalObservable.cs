namespace Prefrontal.Signaling;

/// <summary>
/// Instantiated by <see cref="Signals{TSignal}"/> to allow subscribing to signals of a specific type.
/// </summary>
internal record SignalObservable<TSignal>(Agent Agent) : IObservable<TSignal>
{
	public IDisposable Subscribe(IObserver<TSignal> observer)
	{
		var module = Agent.GetOrAddModule<SignalObserverModule<TSignal>>();
		module.Observers.Add(observer);
		return new DisposeCallback(() =>
		{
			observer.OnCompleted();
			module.Observers.Remove(observer);
		});
	}
}
