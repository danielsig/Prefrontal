namespace Prefrontal.Signaling;

/// <inheritdoc cref="IAsyncSignalInterceptor{TSignal}"/>
public interface ISignalInterceptor<TSignal> : IBaseSignalProcessor<TSignal>
{
	/// <inheritdoc cref="IAsyncSignalInterceptor{TSignal}.InterceptSignalAsync(TSignal)"/>
	Intercept<TSignal> InterceptSignal(TSignal signal);
}
