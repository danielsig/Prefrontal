namespace Prefrontal.Signaling;

/// <inheritdoc cref="IAsyncSignalReceiver{TSignal}"/>
public interface ISignalReceiver<TSignal> : IBaseSignalProcessor<TSignal>
{
	/// <summary>
	/// Receives a signal of type <typeparamref name="TSignal"/>
	/// that was sent by another module
	/// via <see cref="Module.SendSignal{TSignal}(TSignal)"/>
	/// or <see cref="Module.SendSignalAsync{TSignal}(TSignal)"/>.
	/// <br/>
	/// Example:
	/// <code language="csharp">
	/// public class MySignalLoggerModule : Module, ISignalReceiver&lt;MySignal&gt;
	/// {
	/// 	public void ReceiveSignal(MySignal signal)
	/// 	{
	/// 		Debug.Log("Received signal: {signal}", signal);
	/// 	}
	/// }
	/// </code>
	/// </summary>
	/// <param name="signal">The newly received signal.</param>
	void ReceiveSignal(TSignal signal);
}
