namespace Prefrontal.Signaling;

/// <summary>
/// Common interface for all signal processors.
/// <br/>
/// <em><b>Do not implement this interface directly.</b></em>
/// <br/>
/// Modules should instead implement one of the following interfaces:
/// <list type="bullet">
/// 	<item><see cref="ISignalReceiver{TSignal}"/></item>
/// 	<item><see cref="ISignalInterceptor{TSignal}"/></item>
/// 	<item><see cref="IAsyncSignalReceiver{TSignal}"/></item>
/// 	<item><see cref="IAsyncSignalInterceptor{TSignal}"/></item>
/// </list>
/// </summary>
/// <exclude />
public interface IBaseSignalProcessor<TSignal>
{

}
