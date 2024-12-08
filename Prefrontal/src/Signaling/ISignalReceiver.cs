namespace Prefrontal.Signaling;

/// <summary>
/// Lets a module receive signals of a specific type.
/// The order of invocation is determined by the order in which modules are added to the agent
/// unless you specify a different order using the <see cref="Signals{TSignal}" />
/// returned from <see cref="Agent.SignalsOfType{TSignal}">Agent.SignalOfType&lt;<typeparamref name="TSignal"/>&gt;()</see>
/// </summary>
/// <typeparam name="TSignal"></typeparam>
public interface ISignalReceiver<TSignal> : ISignalProcessor<TSignal>
{
	Task ReceiveSignalAsync(TSignal signal);
}
