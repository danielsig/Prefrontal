
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Prefrontal.Modules.Chat;

public abstract class LLM<TMessage>
{
	public string Model = "";
	public List<TMessage> Messages = [];
	public double Temperature = 0.7;
	public int MaxTokens = 200;
	public double TopP = 0.9;
	public double TopK = 0;
	public double PresencePenalty = 0;
	public double FrequencyPenalty = 0;
	public double RepeatPenalty = 1;
	public int Seed = 0;

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public Effort? ReasoningEffort;

	#region abstract and virtual methods
	public abstract Task<List<ModelInfo>> GetAvailableModelsAsync(
		CancellationToken cancellationToken = default
	);
	public abstract IAsyncEnumerable<CompletionChunk> ContinueAsync(
		int n,
		CancellationToken cancellationToken = default
	);
	public virtual async IAsyncEnumerable<TMessage> ContinueAsync(
		[EnumeratorCancellation] CancellationToken cancellationToken = default
	)
	{
		await foreach(var chunk in ContinueAsync(1, cancellationToken))
			if(chunk.Choices?.FirstOrDefault() is CompletionChunk.Choice choice
			&& (choice.Message ?? choice.Delta) is TMessage message)
				yield return message;
	}
	public abstract Task<CompletionChunk> CompleteAsync(
		int n,
		CancellationToken cancellationToken = default
	);
	public virtual async Task<TMessage> CompleteAsync(
		CancellationToken cancellationToken = default
	)
	{
		var chunk = await CompleteAsync(1, cancellationToken);
		if(chunk.Choices?.FirstOrDefault() is CompletionChunk.Choice choice
		&& (choice.Message ?? choice.Delta) is TMessage message)
			return message;
		return EmptyCompletionMessageFactory is not null
			? EmptyCompletionMessageFactory()
			: throw new InvalidOperationException("No completion message was returned.");
	}
	public abstract Task<List<EmbeddingObject>> GetEmbeddingsAsync(
		List<string> input,
		int? dimensions = null,
		string? user = null,
		CancellationToken cancellationToken = default
	);
	public virtual async Task<EmbeddingObject> GetEmbeddingsAsync(
		string input,
		int? dimensions = null,
		string? user = null,
		CancellationToken cancellationToken = default
	)
	{
		var embeddings = await GetEmbeddingsAsync([ input ], dimensions, user, cancellationToken);
		return embeddings.FirstOrDefault()
			?? new EmbeddingObject(Index: 0, Embedding: []);
	}
	#endregion
	
	
	[JsonIgnore]
	public Func<TMessage>? EmptyCompletionMessageFactory
		= Message.FromAssistant("") is TMessage
		? () => Message.FromAssistant("") is TMessage msg ? msg : throw new InvalidOperationException()
		: null;
	
	protected CompletionChunk CreateEmptyCompletionChunk(int n)
		=> new(
			ID: "",
			Created: (long)(DateTime.UtcNow - DateTime.UnixEpoch).TotalSeconds,
			Model: Model,
			Choices: EmptyCompletionMessageFactory is Func<TMessage> makeEmpty
				&& makeEmpty() is TMessage empty
				? [
					new(
						Index: 0,
						FinishReason: "error",
						Message: n == 1 ? empty : default,
						Delta: n == 1 ? default : empty
					)
				]
				: []
		);

	public enum Effort
	{
		Low,
		Medium,
		High,
	}

	public record ModelInfo
	(
		string ID,
		long Created,
		string OwnedBy
	);

	public record CompletionChunk
	(
		string ID,
		long Created,
		string Model,
		ImmutableList<CompletionChunk.Choice> Choices
	)
	{
		public record Choice
		(
			long Index,
			string? FinishReason,
			TMessage? Message,
			TMessage? Delta
		);
	}

	public record EmbeddingObject
	(
		int Index,
		List<double> Embedding
	);
}
