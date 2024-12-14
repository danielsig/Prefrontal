namespace Prefrontal.Signaling;

/// <summary>
/// The return type of signal interceptor methods.
/// Values of type <typeparamref name="TSignal"/> implicitly convert to this type
/// meaning signal interceptor methods can return the signal object directly.
/// To stop the signal from getting processed by subsequent modules,
/// simply <c>return <see cref="Intercept.StopProcessingSignal">Intercept.StopProcessingSignal</see></c>.
/// <para>
/// 	See <see cref="ISignalInterceptor{TSignal}.InterceptSignal(TSignal)"/>
/// 	and <see cref="IAsyncSignalInterceptor{TSignal}.InterceptSignalAsync(TSignal)"/>
/// 	for more information.
/// </para>
/// </summary>
/// <typeparam name="TSignal">The type of signal being intercepted.</typeparam>
public readonly struct Intercept<TSignal>
{
	internal readonly TSignal Signal;
	internal readonly bool ShouldStopProcessing;
	internal Intercept
	(
		TSignal signal,
		bool shouldStop
	)
	{
		Signal = signal;
		ShouldStopProcessing = shouldStop;
	}

	public static implicit operator Intercept<TSignal>(TSignal signal)
		=> new(signal, false);
	public static implicit operator Intercept<TSignal>(Intercept _)
		=> new(default!, true);
}

/// <summary>
/// A container type for the special value
/// <see cref="StopProcessingSignal">Intercept.StopProcessingSignal</see>
/// that can be returned from a signal interceptor method
/// to stop the signal from getting processed by subsequent modules.
/// </summary>
/// <seealso cref="Intercept{TSignal}"/>
/// <seealso cref="ISignalInterceptor{TSignal}.InterceptSignal(TSignal)"/>
/// <seealso cref="IAsyncSignalInterceptor{TSignal}.InterceptSignalAsync(TSignal)"/>
public sealed class Intercept
{
	private Intercept(){ }

	/// <summary>
	/// Return this value within a signal interceptor method
	/// to stop the signal from getting processed by subsequent modules.
	/// </summary>
	public static readonly Intercept StopProcessingSignal = new();
}
