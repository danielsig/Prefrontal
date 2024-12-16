namespace Prefrontal.Signaling;

/// <summary>
/// <list type="bullet">
/// 	<item>
/// 		<see cref="Module">Modules</see> need to implement
/// 		<see cref="IAsyncSignalReceiver{TSignal}" />
/// 		or <see cref="ISignalReceiver{TSignal}" />
/// 		to receive signals of a given <typeparamref name="TSignal"/> type.
/// 	</item>
/// 	<item>
/// 		Modules send signals by
/// 		calling <see cref="Module.SendSignal{TSignal}(TSignal)"/>
/// 		or <see cref="Module.SendSignalAsync{TSignal}(TSignal)"/>.
/// 	</item>
/// 	<item>
/// 		Signal processors are executed <em>in the same order as they were added</em> to the agent,
/// 		<b>unless</b> you specify a different order by calling
/// 		<see cref="Agent.SetSignalProcessingOrder{TSignal}(Func{Agent, List{Module}})"/>.
/// 	</item>
/// </list>
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
	///
	/// Example:
	/// <code language="csharp">
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
