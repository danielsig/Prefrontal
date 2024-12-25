using Prefrontal.Common;
using Prefrontal.Common.Extensions;

namespace Prefrontal.Modules.Chat.Text;

public class ChatModule : Module
{
	public required LLM<Message> LLM
	{
		get => field;
		set => field = value
			?? throw new ArgumentNullException(nameof(LLM));
	}
	public ChatModule(Agent agent) : base(agent)
	{
		LLM = this.RequestLLM()
			?? throw new InvalidOperationException($"LLM Provider is missing on Agent {agent}");

		InterceptSignalsAsync<ChatOperations, (LLM<Message> llm, string textAppended)>(context =>
		{
			foreach(var operation in context.Signal.Operations)
			{
				switch(operation)
				{
					case AppendOperation append:
						LLM.Messages.Add(append.Message);
						break;
					case ConcatOperation concat:
						LLM.Messages.AddRange(concat.Messages);
						break;
					case ClearMessagesOperation _:
						LLM.Messages.Clear();
						break;
					case ReplaceMessagesOperation replace:
						if(replace.Index < LLM.Messages.Count)
						{
							var index = replace.Index.OrAtLeast0();
							LLM.Messages.RemoveRange(index, LLM.Messages.Count - index);
						}
						else LLM.Messages.AddRange(replace.Messages);
						break;
					default:
						throw new InvalidOperationException($"Unknown chat operation {operation}");
				}
			}

			return context.Signal.PostOperation switch
			{
				PostOperation.Continue => LLM
					.ContinueAsync()
					.AppendAsLastMessageOf(LLM),
				PostOperation.Complete => LLM
					.CompleteAsync()
					.AppendAsLastMessageOf(LLM)
					.ToAsyncEnumerable(),
				_ => AsyncEnumerable.FromValue((LLM, "")),
			};
		});
	}
}
