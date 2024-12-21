namespace Prefrontal.Modules.LLM;

public record DialogContinuation(Dialog Dialog) : Dialog(Dialog.Messages)
{
	
}
