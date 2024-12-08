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
	/// <summary>
	/// Receives a signal of type <typeparamref name="TSignal"/>
	/// that was sent by another module
	/// via <see cref="Module.SendSignalAsync{TSignal}(TSignal)"/>
	/// or <see cref="Module.SendSignal{TSignal}(TSignal)"/>.
	/// <br/>
	/// Example:
	/// <code>
	/// public class MySignalLoggerModule : Module, ISignalReceiver&lt;MySignal&gt;
	/// {
	/// 	public Task ReceiveSignalAsync(MySignal signal)
	/// 	{
	/// 		Debug.Log("Received signal: {signal}", signal);
	/// 		return Task.CompletedTask;
	/// 	}
	/// }
	/// </code>
	/// </summary>
	Task ReceiveSignalAsync(TSignal signal);
}
