namespace Prefrontal.Signaling
{
	/// <summary>
	/// Common interface for all signal processors.
	/// <em>Do not implement this interface directly.</em>
	/// Modules should implement
	/// either <see cref="ISignalReceiver{TSignal}"/>
	/// or <see cref="ISignalInterceptor{TSignal}"/>.
	/// </summary>
	public interface ISignalProcessor<TSignal>
	{

	}
}
