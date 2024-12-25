namespace Prefrontal.Modules.Chat;

public static class ModuleExtensions
{
	public static LLM<TMessage>? RequestLLM<TMessage>(this Module module)
		=> module.Request<LLM<TMessage>>();
	public static LLM<Message>? RequestLLM(this Module module)
		=> module.Request<LLM<Message>>();
	
}
