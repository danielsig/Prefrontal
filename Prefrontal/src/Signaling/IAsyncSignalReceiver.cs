namespace Prefrontal.Signaling;

/// <summary>
/// <see cref="Module"/>s need to implement
/// <see cref="IAsyncSignalReceiver{TSignal}" />
/// or <see cref="ISignalReceiver{TSignal}" />
/// to receive signals of that type without intercepting them.
///
/// Modules send signals by
/// calling <see cref="Module.SendSignal{TSignal}(TSignal)"/>.
/// or <see cref="Module.SendSignalAsync{TSignal}(TSignal)"/>
///
/// The order of interception is determined by the order in which modules are added to the agent
/// unless you specify a different order using the <see cref="Agent.SetSignalProcessingOrder{TSignal}(Func{Agent, List{Module}})"/> method.
/// </summary>
/// <typeparam name="TSignal">The type of signals to receive.</typeparam>
public interface IAsyncSignalReceiver<TSignal> : IBaseSignalProcessor<TSignal>
{
	/// <summary>
	/// Receives a signal of type <typeparamref name="TSignal"/>
	/// that was sent by another module
	/// via <see cref="Module.SendSignal{TSignal}(TSignal)"/>
	/// or <see cref="Module.SendSignalAsync{TSignal}(TSignal)"/>.
	/// <br/>
	/// Example:
	/// <code>
	/// public class MySignalLoggerModule : Module, IAsyncSignalReceiver&lt;MySignal&gt;
	/// {
	/// 	public async Task ReceiveSignalAsync(MySignal signal)
	/// 	{
	/// 		await Task.Delay(1000);
	/// 		Debug.Log("Received signal: {signal}", signal);
	/// 	}
	/// }
	/// </code>
	/// </summary>
	/// <param name="signal">The newly received signal.</param>
	Task ReceiveSignalAsync(TSignal signal);
}
