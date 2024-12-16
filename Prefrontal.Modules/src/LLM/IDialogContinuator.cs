namespace Prefrontal.Modules.LLM;


/// <summary>
/// Represents a module that can do LLM chat completions.
/// </summary>
public interface IDialogContinuator
{
	Task<Dialog> ContinueAsync(Dialog context);
	IAsyncEnumerable<DialogMessage> Continue(Dialog dialog);
}
