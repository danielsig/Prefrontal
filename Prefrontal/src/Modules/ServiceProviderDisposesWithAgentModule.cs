namespace Prefrontal.Modules;

/// <summary>
/// A module that disposes of the <see cref="Agent.ServiceProvider"/> when the agent gets disposed.
/// <see cref="Agent.ServiceProvider"/> does not get disposed of when the module is removed from the agent, only when the agent is disposed of.
/// </summary>
[SingletonModule]
public sealed class ServiceProviderDisposesWithAgentModule : Module, IDisposable
{
	public void Dispose()
	{
		if(Agent.ServiceProvider is not IDisposable disposable)
			return;
		
		// figure out if the agent is being disposed of or if the module is simply being removed
		var callstack = new System.Diagnostics.StackTrace();
		var agentType = typeof(Agent);
		var agentIsBeingDisposed = callstack
			.GetFrames()
			.Select(f => f.GetMethod())
			.Any(m
				=> m?.Name == "Dispose"
				&& m.DeclaringType == agentType
			);
		
		// dispose of the services ONLY if the agent is being disposed of
		if(agentIsBeingDisposed)
			disposable.Dispose();
	}
}
