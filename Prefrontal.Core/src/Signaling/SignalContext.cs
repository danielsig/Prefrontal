namespace Prefrontal.Signaling;

/// <summary>
/// The first and only argument passed to signal interceptors
/// to allow them to change the signal value before passing it on to the next processor,
/// or stop processing by not calling <see cref="Next()"/>
/// and finally to allow them to modify the response.
/// </summary>
/// <typeparam name="TSignal">The type of signal the interceptor will process.</typeparam>
/// <typeparam name="TResponse">The type of the response that the interceptor should return.</typeparam>
public class SignalContext<TSignal, TResponse>
{
	/// <summary>
	/// Call this method to pass the signal unchanged to the next processor.
	/// The returned values are the responses from all subsequent processors
	/// and it's up to the interceptor to decide if the responses
	/// should be returned as is, modified or not returned at all.
	/// </summary>
	/// <returns>
	/// 	The responses from all subsequent processors.
	/// </returns>
	public IAsyncEnumerable<TResponse> Next() => _next(Signal);

	/// <summary>
	/// Call this method to pass a different signal to the next processor.
	/// The returned values are the responses from all subsequent processors
	/// and it's up to the interceptor to decide if the responses
	/// should be returned as is, modified or not returned at all.
	/// </summary>
	public IAsyncEnumerable<TResponse> Next(TSignal signal) => _next(signal);

	/// <summary>
	/// The incoming signal that is being processed.
	/// </summary>
	public readonly TSignal Signal;

	#region private/internal
	private readonly Func<TSignal, IAsyncEnumerable<TResponse>> _next;

	internal SignalContext
	(
		TSignal signal,
		Func<TSignal, IAsyncEnumerable<TResponse>> next
	)
	{
		Signal = signal;
		_next = next;
	}
	#endregion
}
