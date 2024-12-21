namespace Prefrontal.Modules.LLM;


/// <summary>
/// Represents a module that can do LLM chat completions.
/// </summary>
public interface IDialogContinuator
{
	Task<DialogContinuation> ContinueAsync(Dialog context);
	IAsyncEnumerable<DialogContinuation> Continue(Dialog dialog);
}
