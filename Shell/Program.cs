using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Prefrontal;
using Prefrontal.Modules.Chat;
using Prefrontal.Modules.Chat.Text;

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.InputEncoding = System.Text.Encoding.UTF8;

using var jarvis = await new Agent
	(
		s => s
		.AddLogging(builder => builder
			.SetMinimumLevel(LogLevel.Warning)
			.AddConsole()
		)
	)
	{
		Name = "Jarvis",
		Description = "Ironman's AI assistant",
	}
	.AddRemoteLLMProvider("http://localhost:1234/v1", "1234")
	.AddModule<ConsoleChatModule>()
	.InitializeAsync();

await jarvis.RunAsync(
	RunningModuleExceptionPolicy.LogAndRerunModule
);

