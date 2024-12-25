namespace Prefrontal.Modules;

public static class ModuleExtensions
{
	public static T? Request<T>(this Module module)
		=> module.Agent
			.SendSignal<Request<T>, T>(new(), true)
			.FirstOrDefault();
	
	public static T? Request<TRequest, T>(this Module module, TRequest request)
		=> module.Agent
			.SendSignal<TRequest, T>(request, true)
			.FirstOrDefault();
	
	public static Task<T?> RequestAsync<T>(this Module module)
		=> module.Agent
			.SendSignalAsync<Request<T>, T>(new())
			.FirstOrDefaultAsync();
	
	public static Task<T?> RequestAsync<TRequest, T>(this Module module, TRequest request)
		=> module.Agent
			.SendSignalAsync<TRequest, T>(request)
			.FirstOrDefaultAsync();
}
