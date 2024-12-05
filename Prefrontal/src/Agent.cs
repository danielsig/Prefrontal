namespace Prefrontal;

/// <summary>
/// An <see cref="Agent"/> manages a collection of modules that work together to achieve a goal.
/// <list type="bullet">
/// 	<item>To create an agent, instantiate it and add modules to it using the <see cref="AddModule{T}(Action{T})"/> method.</item>
/// 	<item>Finally, call <see cref="Initialize"/> to initialize all modules and start the agent.</item>
/// 	<item>When the agent is no longer needed, call <see cref="IDisposable.Dispose"/> to dispose of it.</item>
/// </list>
/// <para>
/// 	If you need to dispose of the <see cref="ServiceProvider"/> when the agent is disposed
/// 	(e.g. if the ServiceProvider was specifically created for that agent),
/// 	simply add <see cref="Modules.ServiceProviderDisposesWithAgentModule"/> to the agent.
/// </para>
/// </summary>
public sealed class Agent : IDisposable
{
	/// <summary>
	/// The service provider that is used to inject dependencies into modules.
	/// </summary>
	public IServiceProvider ServiceProvider { get; init; } = new ServiceCollection().BuildServiceProvider();
    
	/// <summary>
	/// The name of the agent.
	/// </summary>
	public string Name { get; set; } = "";

	/// <summary>
	/// A human readable description of the agent and its purpose.
	/// </summary>
	public string Description { get; set; } = "";

	/// <summary>
	/// All module instances that are currently part of the agent.
	/// </summary>
	public IReadOnlyList<Module> Modules => [.._modules];
	private readonly List<Module> _modules = [];

	/// <summary>
	/// Adds a module of the specified <typeparamref name="T"/> type to the agent.
	/// This calls the constructor of the module type and injects the required services from the <see cref="ServiceProvider"/>.
	/// Use the optional <paramref name="configure"/> action to configure the module right after it has been created but before it is initialized.
	/// Don't forget to call <see cref="Initialize"/> on the agent to initialize all modules after they have all been added.
	/// </summary>
	/// <param name="configure">(optional) callback to configure the module right after it has been created but before it is initialized.</param>
	/// <typeparam name="T">The module type. This must inherit from <see cref="Prefrontal.Module"/></typeparam>
	/// <returns>The current agent for further method chaining.</returns>
	/// <exception cref="InvalidOperationException">
	/// 	Thrown when the type is not a valid module type
	/// 	or when the module could not be instantiated.
	/// </exception>
	public Agent AddModule<T>(Action<T>? configure = null)
	where T : Module
		=> AddModule(
			typeof(T),
			configure is null
				? null
				: m => configure((T)m)
		);
	
	/// <summary>
	/// Adds a module of the specified type to the agent.
	/// This calls the constructor of the module type and injects the required services from the <see cref="ServiceProvider"/>.
	/// Use the optional <paramref name="configure"/> action to configure the module right after it has been created but before it is initialized.
	/// Don't forget to call <see cref="Initialize"/> on the agent to initialize all modules after they have all been added.
	/// </summary>
	/// <param name="type">The module type. This must inherit from <see cref="Prefrontal.Module"/></param>
	/// <param name="configure">(optional) callback to configure the module right after it has been created but before it is initialized.</param>
	/// <returns>The current agent for further method chaining.</returns>
	/// <exception cref="InvalidOperationException">
	/// 	Thrown when the type is not a valid module type
	/// 	or when the module could not be instantiated.
	/// </exception>
	public Agent AddModule(Type type, Action<Module>? configure = null)
	{
		var module = InstantiateModuleWithDependencyInjection(type);
		_modules.Add(module);
		module.Agent = this;
		configure?.Invoke(module);
		return this;
	}

	/// <summary>
	/// Removes <em>all</em> modules of the specified <typeparamref name="T"/> type from the agent
	/// and calls <see cref="IDisposable.Dispose"/> on those that implements <see cref="IDisposable"/>.
	/// </summary>
	/// <typeparam name="T">The module type. This must inherit from <see cref="Prefrontal.Module"/></typeparam>
	/// <returns>True if at least one module of the given type was found and removed, False otherwise.</returns>
	public bool RemoveModule<T>()
	where T : Module
		=> RemoveModule(typeof(T));
	
	/// <summary>
	/// Removes <em>all</em> modules of the specified type from the agent
	/// and calls <see cref="IDisposable.Dispose"/> on those that implements <see cref="IDisposable"/>.
	/// </summary>
	/// <param name="type">The module type. This must inherit from <see cref="Prefrontal.Module"/></param>
	/// <returns>True if at least one module of the given type was found and removed, False otherwise.</returns>
	public bool RemoveModule(Type type)
	{
		var removed = _modules.Extract(c => c.GetType() == type);
		foreach(var component in removed)
			if(removed is IDisposable disposable)
				disposable.Dispose();
		return removed.Count > 0;
	}

	/// <summary>
	/// Removes the specified module from the agent
	/// and calls <see cref="IDisposable.Dispose"/> on it if it implements <see cref="IDisposable"/>.
	/// </summary>
	/// <param name="module">The module to remove.</param>
	/// <returns>True if the module was found and removed, False otherwise.</returns>
	public bool RemoveModule(Module module)
	{
		if(module is null
		|| !_modules.Remove(module))
			return false;
		if(module is IDisposable disposable)
			disposable.Dispose();
		return true;
	}

	/// <summary>
	/// Gets the first module of the specified <typeparamref name="T"/> type
	/// or <see langword="null"/> if the agent does not contain a module of that type.
	/// </summary>
	public T? GetModuleOrDefault<T>()
	where T : Module
		=> _modules.FirstOrDefault(m => m is T) as T;

	/// <summary>
	/// Gets the first module of the specified <typeparamref name="T"/> type
	/// or throws an <see cref="InvalidOperationException"/> if the agent does not contain a module of that type.
	/// </summary>
	public T GetModule<T>()
	where T : Module
		=> GetModuleOrDefault<T>()
		?? throw new InvalidOperationException($"Module of type {typeof(T).ToVerboseString()} not found.");
	
	/// <summary>
	/// Gets all modules of the specified <typeparamref name="T"/> type.
	/// </summary>
	public IEnumerable<T> GetModules<T>()
	where T : Module
		=> _modules.OfType<T>();
	

	/// <summary>
	/// Initializes all modules in the agent.
	/// </summary>
	/// <returns>The current agent.</returns>
	public Agent Initialize()
	{
		foreach(var module in _modules)
			module.Initialize();
		return this;
	}

	/// <summary>
	/// Disposes of the agent and all of its modules.<br/>
	/// All modules that implement <see cref="IDisposable"/> will have their <see cref="IDisposable.Dispose"/> method called.
	/// </summary>
	public void Dispose()
	{
		foreach(var module in _modules)
			if(module is IDisposable disposable)
				disposable.Dispose();
		_modules.Clear();
	}
	
	/// <summary>
	/// Uses the <see cref="ServiceProvider"/> to instantiate a module of the specified type.
	/// </summary>
	/// <param name="type">The module type to instantiate.</param>
	/// <returns>The instantiated module.</returns>
	/// <exception cref="InvalidOperationException">
	/// 	Thrown when the type is not a valid module type
	/// 	or when the module could not be instantiated.
	/// </exception>
	private Module InstantiateModuleWithDependencyInjection(Type type)
	{
		if(type is null
		|| !typeof(Module).IsAssignableFrom(type)
		|| type.IsAbstract)
			throw new InvalidOperationException($"Invalid module type {type.ToVerboseString()}.");
		
		// analyze the constructor of the module
		var constructor
			= type.GetConstructors().FirstOrDefault()
			?? throw new InvalidOperationException($"No constructor found for module of type {type.ToVerboseString()}.");

		// get the parameters of the constructor
		var parameters = constructor.GetParameters();
		var arguments = new object[parameters.Length];
		for(var i = 0; i < parameters.Length; i++)
		{
			var parameter = parameters[i];
			arguments[i]
				= ServiceProvider.GetService(parameter.ParameterType)
				?? throw new InvalidOperationException($"No service found for parameter {parameter.Name} of type {parameter.ParameterType.ToVerboseString()}.");
		}

		// create the module using the constructor and the arguments
		return constructor.Invoke(arguments) as Module
			?? throw new InvalidOperationException($"Could not create module of type {type.ToVerboseString()}.");
	}
}
