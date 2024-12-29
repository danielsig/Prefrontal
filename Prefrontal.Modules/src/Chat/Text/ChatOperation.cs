using System.Collections.Immutable;

namespace Prefrontal.Modules.Chat.Text;

//! This is clearly an over-engineered way to avoid interfaces
//! GIFLENS-https://media4.giphy.com/media/3xz2BLBOt13X9AgjEA/200.gif

public record ChatOperations
(
	ImmutableList<ChatOperation> Operations,
	PostOperation PostOperation = PostOperation.None
);
public abstract record ChatOperation;
public record AppendOperation(Message Message) : ChatOperation;
public record ConcatOperation(params IEnumerable<Message> Messages) : ChatOperation;
public record ClearMessagesOperation() : ChatOperation;
public record ReplaceMessagesOperation(int Index, params IEnumerable<Message> Messages) : ChatOperation;

public enum PostOperation
{
	/// <summary>
	/// Do nothing after the operations.
	/// </summary>
	None,

	/// <summary>
	/// Make the LLM complete the last message.
	/// </summary>
	Complete,

	/// <summary>
	/// Make the LLM continue the last message and stream the completions.
	/// </summary>
	Continue,
}
