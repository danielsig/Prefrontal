using Prefrontal.Modules.LLM;

namespace Prefrontal.Modules;

public static class AgentExtensions
{
	public static Agent AddRemoteLLM(this Agent agent, string endpoint, string apiKey)
	{
		var module = agent.GetOrAddModule<RemoteLLMModule>();
		module.Configure(endpoint, apiKey);
		return agent;
	}
}
