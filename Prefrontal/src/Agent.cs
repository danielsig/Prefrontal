namespace Prefrontal;

/// <summary>
/// An <see cref="Agent"/> manages a collection of modules that work together to achieve a goal.
/// To create an agent, instantiate it and add modules to it using the <see cref="AddModule{T}(Action{T})"/> method.
/// </summary>
public sealed class Agent : IDisposable
{
	public required ServiceProvider Services { get; init; }
    public string Name { get; set; } = "";
	public string Description { get; set; } = "";

	public IReadOnlyList<Module> Modules => [.._modules];
	private readonly List<Module> _modules = [];

	public Agent AddModule<T>(Action<T>? configure = null)
	where T : Module
	{
		// use dependency injection to create the module
		if((Services.GetService<T>() ?? Activator.CreateInstance<T>()) is not T module)
			throw new InvalidOperationException($"Could not create module of type {typeof(T)}.");
		
		_modules.Add(module);
		module.Agent = this;
		module.Initialize();
		configure?.Invoke(module);
		return this;
	}

	public Agent AddModule(Type type, Action<Module>? configure = null)
	{
		if((Services.GetService(type) ?? Activator.CreateInstance(type) as Module) is not Module module)
			throw new InvalidOperationException($"Could not create module of type {type}.");
		_modules.Add(module);
		module.Agent = this;
		module.Initialize();
		configure?.Invoke(module);
		return this;
	}

	public Agent RemoveModule<T>()
	where T : Module => RemoveModule(typeof(T));

	public Agent RemoveModule(Type type)
	{
		var removed = _modules.Extract(c => c.GetType() == type);
		foreach(var component in removed)
			if(removed is IDisposable disposable)
				disposable.Dispose();
		return this;
	}

	public void Dispose()
	{
		foreach(var module in _modules)
			module.Dispose();
	}

	public T? GetModuleOrDefault<T>()
	where T : Module
		=> _modules.OfType<T>().FirstOrDefault();

	public T GetModule<T>()
	where T : Module
		=> GetModuleOrDefault<T>()
		?? throw new InvalidOperationException($"Module of type {typeof(T)} not found.");
	
	public IEnumerable<T> GetModules<T>()
	where T : Module
		=> _modules.OfType<T>();
}
