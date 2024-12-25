namespace Prefrontal.Modules.Chat.Text;

public class ConsoleChatModule(ChatModule _) : Module
{
	protected override async Task RunAsync(CancellationToken cancellationToken)
	{
		Console.WriteLine("Type your message or 'exit' to quit.");
		Console.Write("User: ");
		var input = Console.ReadLine();
		while(input != "exit"
		&& !cancellationToken.IsCancellationRequested)
		{
			if(!string.IsNullOrWhiteSpace(input))
			{
				var message = Message.FromUser(input);
				var operations = new ChatOperations(
					[new AppendOperation(message)],
					PostOperation.Continue
				);

				var prevColor = Console.ForegroundColor;
				var start = true;
				await foreach(var (llm, appended) in SendSignalAsync<ChatOperations, (LLM<Message> LLM, string appended)>(operations))
				{
					Console.ForegroundColor = ConsoleColor.Green;
					if(start)
					{
						start = false;
						Console.Write("Assistant: ");
					}
					Console.Write(appended);
				}
				Console.ForegroundColor = prevColor;
				Console.WriteLine();
			}

			Console.Write("User: ");
			input = Console.ReadLine();
		}
	}
}
