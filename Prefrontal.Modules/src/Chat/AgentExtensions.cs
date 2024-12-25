namespace Prefrontal.Modules.Chat;

public static class AgentExtensions
{
	public static Agent AddRemoteLLMProvider<TMessage>(this Agent agent, string endpoint, string apiKey)
		=> agent.AddProvider<LLM<TMessage>>(() => new RemoteLLM<TMessage>(endpoint, apiKey));
	public static Agent AddRemoteLLMProvider(this Agent agent, string endpoint, string apiKey)
		=> agent.AddProvider<LLM<Message>>(() => new RemoteLLM<Message>(endpoint, apiKey));
}
