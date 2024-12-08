namespace Prefrontal.Signaling;

/// <summary>
/// Lets a module intercept signals of a specific type before they are processed by other modules.
/// The order of interception is determined by the order in which modules are added to the agent
/// unless you specify a different order using the <see cref="Signals{TSignal}" />
/// returned from <see cref="Agent.SignalsOfType{TSignal}">Agent.SignalOfType&lt;<typeparamref name="TSignal"/>&gt;()</see>
/// </summary>
public interface ISignalInterceptor<TSignal> : ISignalProcessor<TSignal>
{
	/// <summary>
	/// Intercepts a signal sent by another module
	/// via <see cref="Module.SendSignalAsync{TSignal}(TSignal)"/>
	/// or <see cref="Module.SendSignal{TSignal}(TSignal)"/>.
	/// <br/>
	/// Interceptors can modify the signal or stop it from propagating to other modules.
	/// You can return either the same input <paramref name="signal"/>
	/// or a new <typeparamref name="TSignal"/> instance
	/// which will then propagate to other modules.
	/// <br/>
	/// You can also <c>return this.StopPropagation();</c> to stop other modules from receiving the signal.
	/// <br/>
	/// When calling <c>this.StopPropagation();</c> You must specify the <typeparamref name="TSignal"/> type parameter
	/// if the module intercepts multiple signal types.
	/// <br/>
	/// Examples:
	/// <code>
	/// public class MySignalInterceptorModule : Module, ISignalInterceptor&lt;MySignal&gt;
	/// {
	/// 	public async Task&lt;Intercepted&lt;MySignal&gt;&gt; InterceptSignalAsync(MySignal signal)
	/// 	{
	/// 		if(signal.ShouldStop)
	/// 			return this.StopPropagation();
	/// 		return signal;
	/// 	}
	/// }
	///
	/// public class SignalABInterceptorModule : Module, ISignalInterceptor&lt;SignalA&gt;, ISignalInterceptor&lt;SignalB&gt;
	/// {
	/// 	public async Task&lt;Intercepted&lt;SignalA&gt;&gt; InterceptSignalAsync(SignalA signal)
	/// 	{
	/// 		if(signal.ShouldStop)
	/// 			return this.StopPropagation&lt;SignalA&gt;();
	/// 		return signal;
	/// 	}
	/// 	public Task&lt;Intercepted&lt;SignalB&gt;&gt; InterceptSignalAsync(SignalB signal)
	/// 	{
	/// 		if(signal.ShouldStop)
	/// 			return this.StopPropagation&lt;SignalB&gt;();
	/// 		return Task.FromResult(signal);
	/// 	}
	/// }
	/// </code>
	/// </summary>
	Task<Intercepted<TSignal>> InterceptSignalAsync(TSignal signal);

}

public static class XSignalInterceptor
{
	/// <inheritdoc cref="ISignalInterceptor{TSignal}.InterceptSignalAsync"/>
	public static Intercepted<TSignal> StopPropagation<TSignal>(this ISignalInterceptor<TSignal> interceptor)
		=> new(default!, SignalInterceptionResult.StopPropagation);
}
