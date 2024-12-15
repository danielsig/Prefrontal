namespace Prefrontal.Signaling;

/// <summary>
/// Instantiated by <see cref="Agent.ObserveSignals{TSignal}()"/>
/// and <see cref="Agent.ObserveSignals{TSignal}(out IObservable{TSignal})"/>
/// which enables subscribing to signals of the given type.
/// <para>
/// 	<em><b>This is an internal class and should not be used directly.</b></em>
/// </para>
/// </summary>
internal record SignalObservable<TSignal>(Agent Agent) : IObservable<TSignal>
{
	public IDisposable Subscribe(IObserver<TSignal> observer)
	{
		var module = Agent.GetOrAddModule<SignalObserverModule<TSignal>>();
		module.AddObserver(observer);
		return new DisposeCallback(() =>
			module.RemoveObserver(observer)
		);
	}
}
