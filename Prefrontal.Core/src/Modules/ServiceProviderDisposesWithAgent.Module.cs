namespace Prefrontal.Modules;

/// <summary>
/// A module that disposes of the agent's <see cref="Agent.ServiceProvider"/>
/// when the agent itself gets disposed. It does not have any effects
/// if the module is simply being removed from the agent.
/// </summary>
[SingletonModule]
public sealed class ServiceProviderDisposesWithAgentModule : Module, IDisposable
{
	public void Dispose()
	{
		if(Agent.ServiceProvider is not IDisposable disposable)
			return;
		
		if(Agent.State == AgentState.Disposing)
			disposable.Dispose();
	}
}
