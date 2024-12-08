namespace Prefrontal.Signaling;

/// <summary>
/// Determines if the agent should continue letting the signal propagate to other
/// <see cref="ISignalInterceptor{TSignal}"/>s and <see cref="ISignalReceiver{TSignal}"/>s
/// or if it should stop the propagation.
/// You do not need to use this enum directly.
/// See <see cref="ISignalInterceptor{TSignal}.InterceptSignalAsync(TSignal)"/> for more information.
/// </summary>
public enum SignalInterceptionResult
{
	Continue,
	StopPropagation,
}
