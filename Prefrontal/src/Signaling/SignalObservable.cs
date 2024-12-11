namespace Prefrontal.Signaling;

/// <summary>
/// Instantiated by <see cref="Agent.ObserveSignals{TSignal}()"/>
/// and <see cref="Agent.ObserveSignals{TSignal}(out IObservable{TSignal})"/>
/// which enables subscribing to signals of the given type.
/// <para>
/// 	This is an internal class and should not be used directly.
/// </para>
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
