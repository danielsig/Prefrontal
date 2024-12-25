namespace Prefrontal.Modules.Chat.Text;

public static class AsyncEnumerableExtensions
{
	public static async IAsyncEnumerable<(LLM<Message> llm, string textAppended)> AppendAsLastMessageOf<T>(
		this IAsyncEnumerable<T> source,
		LLM<Message> llm
	)
	where T : Message
	{
		var assistant = Message.FromAssistant("");
		llm.Messages.Add(assistant);
		await foreach(var item in source)
		{
			assistant.Content += item.Content;
			yield return (llm, item.Content);
		}
	}

	public static async Task<(LLM<Message> llm, string textAppended)> AppendAsLastMessageOf<T>(
		this Task<T> source,
		LLM<Message> llm
	)
	where T : Message
	{
		var assistant = Message.FromAssistant("");
		llm.Messages.Add(assistant);
		var item = await source;
		assistant.Content += item.Content;
		return (llm, item.Content);
	}
}
