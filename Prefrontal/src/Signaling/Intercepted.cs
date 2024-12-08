namespace Prefrontal.Signaling;

/// <summary>
/// The return value of a signal interceptor.
/// You do not need to instantiate this directly.
/// <br/>
/// See <see cref="ISignalInterceptor{TSignal}.InterceptSignalAsync(TSignal)"/> for more information.
/// </summary>
public readonly struct Intercepted<TSignal>(TSignal signal, SignalInterceptionResult result = SignalInterceptionResult.Continue)
{
	public readonly TSignal Signal = signal;
	public readonly SignalInterceptionResult Result = result;
	
	public static implicit operator Intercepted<TSignal>(TSignal signal) => new(signal);
	public static implicit operator Intercepted<TSignal>(SignalInterceptionResult result) => new(default!, result);
	public static implicit operator Task<Intercepted<TSignal>>(Intercepted<TSignal> intercepted) => Task.FromResult(intercepted);
}
