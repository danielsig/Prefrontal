namespace Prefrontal;

/// <summary>
/// An <see cref="Agent"/> manages a collection of modules that work together to achieve a goal.
/// <list type="bullet">
/// 	<item>To create an agent simply instantiate it with the <see langword="new"/> operator.</item>
/// 	<item>Add modules to the agent by chaining <see cref="AddModule{T}(Action{T}?)"/> method calls.</item>
/// 	<item>Finally call <see cref="Initialize"/> to initialize all modules and start the agent.</item>
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
	private bool _initialized = false;

	/// <summary>
	/// Adds a module of the specified <typeparamref name="T"/> type to the agent.
	/// This is the generic version of <see cref="AddModule(Type, Action{Module})"/>.
	/// The module type's constructor gets called and the required services are injected from the <see cref="ServiceProvider"/>.
	/// The module's constructor can also take the agent itself as a parameter and even other modules that it requires.
	/// The optional <paramref name="configure"/> action can be used to configure the module before it is initialized.
	/// Don't forget to call <see cref="Initialize"/> on the agent
	/// to initialize all modules after you've added all the modules.
	/// <para>
	/// 	All modules that are added after the agent is initialized
	/// 	are initialized at the same time as they are added.
	/// </para>
	/// </summary>
	/// <typeparam name="T">
	/// 	The module type.
	/// 	This must inherit from <see cref="Prefrontal.Module"/>
	/// </typeparam>
	/// <param name="configure">
	/// 	(optional) callback to configure the module before it is initialized.
	/// </param>
	/// <returns>The current agent for further method chaining.</returns>
	/// <exception cref="InvalidOperationException">
	/// 	Thrown when the type is not a valid module type
	/// 	or when the module could not be created.
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
	/// This is the non-generic version of <see cref="AddModule{T}(Action{T})"/>.
	/// The module type's constructor is called and the required services are injected from the <see cref="ServiceProvider"/>.
	/// The module's constructor can also take the agent itself as a parameter and even other modules that it requires.
	/// The optional <paramref name="configure"/> action can be used to configure the module before it is initialized.
	/// Don't forget to call <see cref="Initialize"/> on the agent
	/// to initialize all modules after you've added all the modules.
	/// <para>
	/// 	All modules that are added after the agent is initialized
	/// 	are initialized at the same time as they are added.
	/// </para>
	/// </summary>
	/// <param name="type">
	/// 	The module type.
	/// 	This must inherit from <see cref="Prefrontal.Module"/>
	/// </param>
	/// <param name="configure">
	/// 	(optional) callback to configure the module before it is initialized.
	/// </param>
	/// <returns>The current agent for further method chaining.</returns>
	/// <exception cref="InvalidOperationException">
	/// 	Thrown when the type is not a valid module type
	/// 	or when the module could not be created.
	/// </exception>
	public Agent AddModule(Type type, Action<Module>? configure = null)
	{
		AddModule(type, configure);
		return this;
	}
	private Module? AddModuleInternal(Type type, Action<Module>? configure = null)
	{
		if(type is null)
			throw new ArgumentNullException(nameof(type));
		
		var typeData = GetModuleTypeData(type);

		// check if the module is a singleton and if it already exists
		if(typeData.IsSingleton
		&& GetModuleOrDefault(type) is Module singleton)
		{
			configure?.Invoke(singleton); // configure the singleton
			return singleton; // return the singleton
		}
		
		// store the modules before adding the new one (for rollback in case of failure)
		Module[] modulesBefore = [.._modules];

		try
		{
			// instantiate the module and add it to the agent
			var module = InstantiateModuleWithDependencyInjection(type);
			_modules.Add(module);
			module.Agent = this;

			// check for required modules (configured by the RequiredModuleAttribute)
			foreach(var member in typeData.RequiredModules)
			{
				var requiredModule
					= GetModuleOrDefault(member.ModuleType)
					?? AddModuleInternal(member.ModuleType)
					?? throw new InvalidOperationException(
						$"Required module of type {member.ModuleType.ToVerboseString()} not found."
					);
				member.SetModule(module, requiredModule);
			}
			
			// invoke callbacks
			configure?.Invoke(module);
			if(_initialized)
				module.Initialize();

			return module;
		}
		catch(Exception ex)
		{
			// rollback the changes
			var modulesToRemove = _modules.Except(modulesBefore).ToList();
			foreach(var moduleBefore in modulesToRemove)
				RemoveModule(moduleBefore);
			
			// rethrow the exception
			throw new InvalidOperationException(
				$@"Failed to {(
					_initialized
						? "configure or initialize"
						: "configure"
				)} module {
					type.ToVerboseString()
				} on agent {
					Name
				}.",
				ex
			);
		}
	}

	/// <summary>
	/// Removes <em>all</em> modules of the specified <typeparamref name="T"/> type from the agent
	/// and calls <see cref="IDisposable.Dispose"/> on those that implements <see cref="IDisposable"/>.
	/// </summary>
	/// <typeparam name="T">
	/// 	The module type.
	/// 	This must inherit from <see cref="Prefrontal.Module"/>
	/// </typeparam>
	/// <returns>
	/// 	True if at least one module of the given type was found and removed,
	/// 	False otherwise.
	/// </returns>
	public bool RemoveModule<T>()
	where T : Module
		=> RemoveModule(typeof(T));
	
	/// <summary>
	/// Removes <em>all</em> modules of the specified type from the agent
	/// and calls <see cref="IDisposable.Dispose"/> on those that implements <see cref="IDisposable"/>.
	/// </summary>
	/// <param name="type">
	/// 	The module type.
	/// 	This must inherit from <see cref="Prefrontal.Module"/>
	/// </param>
	/// <returns>
	/// 	True if at least one module of the given type was found and removed,
	/// 	False otherwise.
	/// </returns>
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
	/// Gets the first module of the specified <paramref name="type"/> type
	/// or <see langword="null"/> if the agent does not contain a module of that type.
	/// </summary>
	public Module? GetModuleOrDefault(Type type)
		=> _modules.FirstOrDefault(m => m.GetType() == type);

	/// <summary>
	/// Gets the first module of the specified <typeparamref name="T"/> type
	/// or throws an <see cref="InvalidOperationException"/>
	/// if the agent does not contain a module of that type.
	/// </summary>
	public T GetModule<T>()
	where T : Module
		=> GetModuleOrDefault<T>()
		?? throw new InvalidOperationException(
			$"Module of type {typeof(T).ToVerboseString()} not found."
		);
	
	/// <summary>
	/// Gets the first module of the specified <paramref name="type"/> type
	/// or throws an <see cref="InvalidOperationException"/>
	/// if the agent does not contain a module of that type.
	/// </summary>
	public Module GetModule(Type type)
		=> GetModuleOrDefault(type)
		?? throw new InvalidOperationException(
			$"Module of type {type.ToVerboseString()} not found."
		);
	
	/// <summary>
	/// Gets all modules of the specified <typeparamref name="T"/> type.
	/// </summary>
	public IEnumerable<T> GetModules<T>()
	where T : Module
		=> _modules.OfType<T>();
	

	/// <summary>
	/// Initializes all modules on the agent.
	/// </summary>
	/// <returns>The current agent.</returns>
	public Agent Initialize()
	{
		var exceptions = new List<Exception>();
		foreach(var module in _modules)
			try
			{
				module.Initialize();
			}
			catch(Exception ex)
			{
				exceptions.Add(ex);
			}
		_initialized = true;
		if(exceptions.Count > 0)
			throw new AggregateException(
				$"Failed to initialize modules on agent {Name}.",
				exceptions
			);
		return this;
	}

	/// <summary>
	/// Disposes of the agent and all of its modules.<br/>
	/// All modules that implement <see cref="IDisposable"/>
	/// will have their <see cref="IDisposable.Dispose"/> method called.
	/// </summary>
	public void Dispose()
	{
		var exceptions = new List<Exception>();
		foreach(var module in _modules)
			if(module is IDisposable disposable)
				try
				{
					disposable.Dispose();
				}
				catch(Exception ex)
				{
					exceptions.Add(ex);
				}
		_modules.Clear();
		if(exceptions.Count > 0)
			throw new AggregateException(
				$"Failed to dispose of modules on agent {Name}.",
				exceptions
			);
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
		if(!typeof(Module).IsAssignableFrom(type)
		|| type.IsAbstract)
			throw new InvalidOperationException($"Invalid module type {type.ToVerboseString()}.");
		
		// analyze the constructor of the module
		var constructor
			= type.GetConstructors().FirstOrDefault()
			?? throw new InvalidOperationException($"No constructor found for module of type {type.ToVerboseString()}.");

		// get the parameters of the constructor
		var agentType = typeof(Agent);
		var moduleType = typeof(Module);
		var parameters = constructor.GetParameters();
		var arguments = new object[parameters.Length];
		for(var i = 0; i < parameters.Length; i++)
		{
			var param = parameters[i];
			var paramType = param.ParameterType;
			arguments[i]
				= ServiceProvider.GetService(paramType)
				?? (paramType == agentType ? this as object : null)
				?? (
					moduleType.IsAssignableFrom(paramType)
						? GetModuleOrDefault(paramType)
							?? AddModuleInternal(paramType)
						: null
				)
				?? throw new InvalidOperationException($"No service found for parameter {param.Name} of type {paramType.ToVerboseString()}.");
		}

		// create the module using the constructor and the arguments
		return constructor.Invoke(arguments) as Module
			?? throw new InvalidOperationException($"Could not create module of type {type.ToVerboseString()}.");
	}

	private static readonly Dictionary<Type, ModuleTypeData> _moduleTypeDataCache = [];
	private static ModuleTypeData GetModuleTypeData(Type type)
	{
		if(_moduleTypeDataCache.TryGetValue(type, out var data))
			return data;
		
		var classAttributes = type
			.GetCustomAttributes(typeof(IModuleAttribute), true)
			.Cast<IModuleAttribute>()
			.ToList();
		var memberAttributes = type
			.GetMembers()
			.Select(member =>
			(
				member,
				attributes: member
					.GetCustomAttributes(typeof(IModuleMemberAttribute), true)
					.Cast<IModuleMemberAttribute>()
					.ToList()
			))
			.Where(t => t.attributes.Count > 0)
			.ToList();
		
		var isSingleton = classAttributes.Any(a => a is SingletonModuleAttribute);
		var requiredModules = memberAttributes
			.Where(t => t.attributes.Any(a => a is RequiredModuleAttribute))
			.Select(t => t.member switch
				{
					System.Reflection.FieldInfo field
						=> new RequiredModule(
							field.FieldType,
							(module, requiredModule) => field.SetValue(module, requiredModule)
						),
					System.Reflection.PropertyInfo property
						=> new RequiredModule(
							property.PropertyType,
							(module, requiredModule) => property.SetValue(module, requiredModule)
						),
					_ => null!,
				}
			)
			.Where(t
				=> t.ModuleType is not null
				&& t.ModuleType.IsSubclassOf(typeof(Module))
				&& t.ModuleType != type
			)
			.ToList();
		
		return _moduleTypeDataCache[type]
			= new ModuleTypeData(
				isSingleton,
				requiredModules
			);
	}

	private record ModuleTypeData
	(
		bool IsSingleton,
		List<RequiredModule> RequiredModules
	);
	private record RequiredModule(
		Type ModuleType,
		Action<object, object> SetModule
	);
}
