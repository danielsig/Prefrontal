namespace Prefrontal.Modules;

public static class AgentExtensions
{
	public static Agent AddProvider<TRequest, T>(this Agent agent, Func<TRequest, T> factory)
		=> agent.AddModule(a => new ProviderModule<TRequest, T>(agent, factory));
	public static Agent AddProvider<T>(this Agent agent, Func<T> factory)
		=> agent.AddModule(a => new ProviderModule<T>(agent, factory));
}
