using System.Collections.Immutable;

namespace Prefrontal.Modules.LLM;

public record Dialog(ImmutableList<DialogMessage> Messages)
{
	public Dialog Add(DialogMessage message) => new(Messages.Add(message));
}
