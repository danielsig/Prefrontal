using Microsoft.Extensions.Configuration;

namespace Prefrontal.Modules;

// TODO: Make this class work by expecting an URL to an OpenAI compatible API endpoint, and a token to authenticate with.
/// <summary>
/// Work in progress.
/// </summary>
internal class LLMEndpointModule : Module
{
	private string? OpenAICompatibleURL { get; set; }
	private string? APIKey { get; set; }
	public LLMEndpointModule(IConfiguration configuration)
	{
		configuration
			.GetSection("LLM")
			.Bind(this);
	}
	protected internal override async Task InitializeAsync()
	{
		await Task.CompletedTask;
	}
}
