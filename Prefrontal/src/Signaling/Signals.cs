namespace Prefrontal.Signaling;

/// <summary>
/// Provides access to signals of a specific type on a given agent.
/// </summary>
/// <typeparam name="TSignal">The type of signal to access.</typeparam>
/// <param name="agent">The agent to access signals on.</param>
public readonly struct Signals<TSignal>(Agent agent)
{
	private readonly Agent _agent = agent;

	/// <summary>
	/// Gets an observable for all future signals of the specified type emitted on the agent.
	/// </summary>
	/// <returns>The observable.</returns>
	public IObservable<TSignal> GetObservable()
		=> new SignalObservable<TSignal>(_agent);
	
	/// <summary>
	/// Gets an observable for all future signals of the specified type emitted on the agent.
	/// </summary>
	/// <param name="observable">A reference to assign the observable to.</param>
	/// <returns>The agent for further configuration.</returns>
	public Agent GetObservable(ref IObservable<TSignal> observable)
	{
		observable = GetObservable();
		return _agent;
	}
	
	/// <summary>
	/// Specifies the order in which modules must process signals of the given type.
	/// <br/>
	/// Example:
	/// <code>
	/// var myAgent = new Agent()
	/// 	.AddModule&lt;LastModule&gt;()
	/// 	.AddModule&lt;ThirdModule&gt;()
	/// 	.AddModule&lt;SecondModule&gt;()
	/// 	.AddModule&lt;FirstModule&gt;()
	/// 	.SignalsOfType&lt;MySignal&gt;().AreProcessedInThisOrder(a =>
	/// 	[
	/// 		a.GetModule&lt;FirstModule&gt;(),
	/// 		a.GetModule&lt;SecondModule&gt;(),
	/// 		a.GetModule&lt;ThirdModule&gt;(),
	/// 		a.GetModule&lt;LastModule&gt;(),
	/// 	])
	/// 	.Initialize();
	/// </code>
	/// </summary>
	/// <param name="getModuleOrder">A function that returns the order in which modules should process the signals.</param>
	/// <seealso cref="ISignalInterceptor{TSignal}"/>
	/// <seealso cref="ISignalReceiver{TSignal}"/>
	public Agent AreProcessedInThisOrder(Func<Agent, List<Module>> getModuleOrder)
	{
		var moduleOrder = getModuleOrder(_agent);
		foreach(var module in moduleOrder)
		{
			if(module is not ISignalProcessor<TSignal>)
				throw new ArgumentException($"Module {module} does not implement {typeof(ISignalProcessor<TSignal>).ToVerboseString()}.");
			if(module.Agent != _agent)
				throw new ArgumentException($"Module {module} does not belong to the agent.");
		}
		var type = typeof(TSignal);
		if(_agent.SignalProcessorPriorityPerType.TryGetValue(type, out var before))
			_agent.SignalProcessorPriorityPerType[type] = [..moduleOrder, ..before.Except(moduleOrder)];
		else
			_agent.SignalProcessorPriorityPerType[type] = [..moduleOrder];
		return _agent;
	}
}
