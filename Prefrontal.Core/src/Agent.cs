﻿using System.Collections.Concurrent;
using Microsoft.Extensions.Logging.Abstractions;
using Prefrontal.Reactive;
using static Prefrontal.AgentState;

namespace Prefrontal;

/// <summary>
/// An agent manages a collection of <see cref="Module">Modules</see> that work together to achieve a goal.
/// <br/>
/// <h5>Getting Started</h5>
/// <list type="bullet">
/// 	<item>
/// 		To create an agent, simply instantiate it
/// 		with the <see langword="new"/> operator.
/// 	</item>
/// 	<item>
/// 		Add modules to the agent by chaining
/// 		<see cref="AddModule{T}(Action{T}?)">AddModule&lt;T&gt;()</see> method calls.
/// 	</item>
/// 	<item>
/// 		Finally call <see cref="Initialize">Initialize()</see> or <see cref="InitializeAsync">InitializeAsync()</see>
/// 		to initialize all modules and start the agent.
/// 	</item>
/// 	<item>
/// 		When the agent is no longer needed,
/// 		call <see cref="IDisposable.Dispose"/> to dispose of it.
/// 	</item>
/// </list>
/// <h5 id="agent_lifecycle">The Agent Lifecycle</h5>
/// <list type="table">
/// 	<listheader>
/// 		<term><see cref="State">Agent.State</see></term>
/// 		<description>Available Actions</description>
/// 	</listheader>
/// 	<item>
/// 		<term><see cref="Uninitialized"/></term>
/// 		<description>
/// 			<list type="bullet">
/// 				<item><span style="color: #c5ffbf;">Modules can be added.</span></item>
/// 				<item><span style="color: #c5ffbf;">Modules can be removed.</span></item>
/// 				<item><span style="color: #c5ffbf;">Modules can send and receive signals.</span></item>
/// 				<item><span style="color: #c5ffbf;">Signal processing order can be changed.</span></item>
/// 			</list>
///			</description>
/// 	</item>
/// 	<item>
/// 		<term><see cref="Initializing"/></term>
/// 		<description>
/// 			<list type="bullet">
/// 				<item><span style="color: #c5ffbf;">Modules can be added (new modules are initialized immediately).</span></item>
/// 				<item><span style="color: #e84035;">Modules <b>cannot</b> be removed.</span></item>
/// 				<item><span style="color: #c5ffbf;">Modules can send and receive signals.</span></item>
/// 				<item><span style="color: #c5ffbf;">Signal processing order can be changed.</span></item>
/// 			</list>
///			</description>
/// 	</item>
/// 	<item>
/// 		<term><see cref="Initialized"/></term>
/// 		<description>
/// 			<list type="bullet">
/// 				<item><span style="color: #c5ffbf;">Modules can be added (new modules are initialized immediately).</span></item>
/// 				<item><span style="color: #c5ffbf;">Modules can be removed.</span></item>
/// 				<item><span style="color: #c5ffbf;">Modules can send and receive signals.</span></item>
/// 				<item><span style="color: #c5ffbf;">Signal processing order can be changed.</span></item>
/// 			</list>
///			</description>
/// 	</item>
/// 	<item>
/// 		<term><see cref="Disposing"/></term>
/// 		<description>
///				<list type="bullet">
/// 				<item><span style="color: #e84035;">Modules <b>cannot</b> be added.</span></item>
/// 				<item><span style="color: #e84035;">Modules <b>cannot</b> be removed.</span></item>
/// 				<item><span style="color: #c5ffbf;">Modules can send and receive signals.</span></item>
/// 				<item><span style="color: #c5ffbf;">Signal processing order can be changed.</span></item>
/// 			</list>
///			</description>
/// 	</item>
/// 	<item>
/// 		<term><see cref="Disposed"/></term>
/// 		<description>
///				<list type="bullet">
/// 				<item><span style="color: #e84035;">Modules <b>cannot</b> be added.</span></item>
/// 				<item><span style="color: #e84035;">Modules <b>cannot</b> be removed.</span></item>
/// 				<item><span style="color: #e84035;">Modules <b>cannot</b> send nor receive signals.</span></item>
/// 				<item><span style="color: #e84035;">Signal processing order <b>cannot</b> be changed.</span></item>
/// 			</list>
///			</description>
/// 	</item>
/// </list>
/// <para>
/// 	<h5><see cref="ServiceProvider"/> Disposal</h5>
/// 	If you need to dispose of the <see cref="ServiceProvider"/> when the agent is disposed
/// 	(e.g. if the ServiceProvider was specifically created for that agent),
/// 	simply add <see cref="ServiceProviderDisposesWithAgentModule"/> to the agent.
/// </para>
/// <br/>
/// </summary>
public partial class Agent : IDisposable
{
	/// <summary>
	/// Creates a new agent without a <see cref="ServiceProvider"/>.
	/// </summary>
	public Agent()
	{
		ServiceProvider = EmptyServiceProvider.Instance;
		Debug = DefaultLogger;
		_state = new(Debug);
	}

	/// <summary>
	/// Creates a new agent with the specified <paramref name="serviceProvider"/>.
	/// </summary>
	/// <param name="serviceProvider">The service provider to use when solving dependencies.</param>
	/// <exception cref="ArgumentNullException"></exception>
	/// <exception cref="InvalidOperationException"></exception>
	public Agent(IServiceProvider serviceProvider)
	{
		ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		Debug = serviceProvider.GetService<ILogger<Agent>>()
			?? serviceProvider.GetService<ILogger>()
			?? serviceProvider.GetService<ILoggerProvider>()?.CreateLogger("Agent")
			?? DefaultLogger;
		_state = new(Debug);
	}

	/// <summary>
	/// Creates a new agent and uses the specified
	/// <paramref name="configureServices"/> action
	/// to construct a <see cref="ServiceProvider"/>.
	/// <para>
	/// 	This constructor also adds a
	/// 	<see cref="ServiceProviderDisposesWithAgentModule"/>
	/// 	to the agent to properly dispose of the <see cref="ServiceProvider"/>
	/// 	when the agent gets disposed.
	/// </para>
	/// </summary>
	public Agent(Action<ServiceCollection> configureServices)
	: this(
		new ServiceCollection()
			.Tap(configureServices)
			.BuildServiceProvider()
	)
	{
		AddModule<ServiceProviderDisposesWithAgentModule>();
	}
	#region Fields & Properties

	/// <summary>
	/// The service provider that is used to inject dependencies into modules.
	/// </summary>
	public IServiceProvider ServiceProvider { get; private init; }

	/// <summary>
	/// The logger that is used to log debug messages.
	/// </summary>
	protected internal ILogger Debug;
	private static readonly ILogger DefaultLogger
		= NullLoggerFactory.Instance.CreateLogger("Agent");

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
	/// The current state of the agent.
	/// Agents start in the <see cref="Uninitialized"/> state.
	/// Calling <see cref="Initialize">Initialize()</see>/<see cref="InitializeAsync">InitializeAsync()</see>
	/// sets the state to <see cref="Initializing"/>
	/// while modules are being initialized and to <see cref="Initialized"/> when they are done.
	/// Calling <see cref="Dispose"/> sets the state to <see cref="Disposing"/>
	/// while modules are being disposed of and to <see cref="Disposed"/> when done.
	/// </summary>
	public AgentState State
	{
		get => _state.State;
		private set => _state.State = value;
	}
	private readonly AgentStateObservable _state;

	/// <summary>
	/// An observable stream of the agent's state.
	/// Subscribing will immediately return the current state.
	/// </summary>
	/// <seealso cref="State"/>
	/// <seealso cref="Initialization"/>
	public IObservable<AgentState> StateObservable => _state;

	/// <summary>
	/// Provides a task that completes when the agent is no longer <see cref="Uninitialized"/> or <see cref="Initializing"/>.
	/// <br/>
	/// This is handy when a piece of code that didn't call <see cref="InitializeAsync"/>
	/// needs to wait for initialization to complete.
	/// </summary>
	/// <seealso cref="State"/>
	/// <seealso cref="StateObservable"/>
	public Task Initialization => State is Uninitialized or Initializing
		? Thru(new TaskCompletionSource(), t =>
		{
			IDisposable? disposable = null;
			var done = false;
			disposable = _state.Subscribe(new AnonymousObserver<AgentState>(
				state =>
				{
					if(state is Uninitialized or Initializing)
						return;
					done = true;
					disposable?.Dispose();
					t.TrySetResult();
				},
				ex => t.TrySetException(ex),
				() =>
				{
					if(!done)
						t.TrySetResult();
				}
			));
			return t.Task;
		})
		: Task.CompletedTask;

	#endregion

	#region AddModule

	/// <summary>
	/// Adds a module of the specified type to the agent.
	/// The module type's constructor gets called and the required services are injected from the <see cref="ServiceProvider"/>.
	/// The module's constructor can also take the agent itself as a parameter and even other modules that it requires.
	/// The optional <paramref name="configure"/> action can be used to configure the module before it is initialized.
	/// Don't forget to <see cref="InitializeAsync">initialize</see> the agent after you've added all the modules.
	/// <para>
	/// 	When a module is added <em>after</em> the agent is initialized it gets initialized immediately.
	/// </para>
	/// </summary>
	/// <typeparam name="T">
	/// 	The module type.
	/// 	This must inherit from <see cref="Module"/>.
	/// </typeparam>
	/// <param name="configure">
	/// 	(optional) callback to configure the module before it is initialized.
	/// </param>
	/// <returns>The agent for further method chaining.</returns>
	/// <exception cref="InvalidOperationException">
	/// 	Thrown when the type is not a valid module type
	/// 	or when the module could not be created.
	/// </exception>
	public Agent AddModule<T>(Action<T>? configure = null)
	where T : Module
	{
		AddModuleInternal(
			typeof(T),
			configure is null
				? null
				: m => configure((T)m)
		);
		return this;
	}

	/// <summary>
	/// Adds a module using a factory function to create the module.
	/// Don't forget to <see cref="InitializeAsync">initialize</see> the agent after you've added all the modules.
	/// <para>
	/// 	When a module is added <em>after</em> the agent is initialized it gets initialized immediately.
	/// </para>
	/// </summary>
	/// <typeparam name="T">
	/// 	The module type.
	/// 	This must inherit from <see cref="Module"/>.
	/// </typeparam>
	/// <param name="createModule">
	/// 	A function that creates the module.
	/// </param>
	/// <returns>The agent for further method chaining.</returns>
	/// <exception cref="InvalidOperationException">
	/// 	Thrown when the type is not a valid module type
	/// 	or when the module could not be created.
	/// </exception>
	public Agent AddModule<T>(Func<Agent, T> createModule)
	where T : Module
	{
		AddModuleInternal(
			typeof(T),
			null,
			createModule
		);
		return this;
	}
	
	/// <inheritdoc cref="AddModule{T}(Action{T}?)"/>
	/// <param name="moduleType">
	/// 	The module type.
	/// 	A module must be a concrete class that inherits from <see cref="Module"/>.
	/// </param>
	public Agent AddModule(Type moduleType, Action<Module>? configure = null)
	{
		AddModuleInternal(moduleType, configure);
		return this;
	}
	private Module AddModuleInternal(
		Type type,
		Action<Module>? configure = null,
		Func<Agent, Module>? createModule = null
	)
	{
		if(State is Disposed or Disposing)
			throw new ObjectDisposedException("Cannot add modules to a disposed agent.");
		
		ArgumentNullException.ThrowIfNull(type);

		var typeData = GetModuleTypeData(type);

		// check if the module is a singleton and if it already exists
		if(typeData.IsSingleton
		&& GetModuleOrDefault(type) is Module singleton)
		{
			configure?.Invoke(singleton); // configure the singleton
			return singleton; // return the singleton
		}

		// store the modules before adding the new one (for rollback in case of failure)
		Module[] modulesBefore = [.. _modules];

		try
		{
			// instantiate the module and add it to the agent
			var module = createModule?.Invoke(this) ?? InstantiateModuleWithDependencyInjection(type);
			_modules.Add(module);
			module.Agent = this;

			// check for required modules (configured by the RequiredModuleAttribute)
			foreach (var member in typeData.RequiredModules)
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
			if(State is Initialized or Initializing)
				Task.Run(async () =>
				{
					await module.InitializeAsync(); // call InitializeAsync
					_updateRunningModules?.TrySetResult(); // call RunAsync (if running)
				});


			return module;
		}
		catch(Exception ex)
		{
			// rollback the changes
			var modulesToRemove = _modules.Except(modulesBefore).ToList();
			foreach (var moduleBefore in modulesToRemove)
				RemoveModule(moduleBefore);

			// rethrow the exception
			throw new InvalidOperationException(
				$@"Failed to {(
					State is Initialized or Initializing
						? "configure or initialize"
						: "configure"
				)} module {type.ToVerboseString()} on agent {Name}.",
				ex
			);
		}
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
		// if type is not a concrete module type
		if(!typeof(Module).IsAssignableFrom(type)
		|| type.IsAbstract)
		{
			// then try to resolve it from the service provider
			if(ServiceProvider.GetService(type) is Module module)
				return module;
			
			// else throw an exception
			throw new InvalidOperationException($"Invalid module type {type.ToVerboseString()}.");
		}
		
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
				?? (moduleType.IsAssignableFrom(paramType)
					? GetModuleOrDefault(paramType)
						?? AddModuleInternal(paramType)
					: null
				)
				?? (paramType.IsAbstract || paramType.IsInterface
					? GetModuleOrDefault(paramType)
					: null
				)
				?? throw new InvalidOperationException(
					$@"Could not resolve dependency for {
						paramType.ToVerboseString()
					} {
						param.Name
					} in constructor of {
						type.ToVerboseString()
					}."
				);
			if(arguments[i] is Module)
				_requiredBy
					.GetOrAdd(arguments[i].GetType())
					.Add(type);
		}

		// create the module using the constructor and the arguments
		return constructor.Invoke(arguments) as Module
			?? throw new InvalidOperationException($"Could not create module of type {type.ToVerboseString()}.");
	}

	private static readonly Dictionary<Type, ModuleTypeData> _moduleTypeDataCache = [];
	private static readonly Dictionary<Type, List<Type>> _requiredBy = [];
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
					System.Reflection.FieldInfo info
						=> new RequiredModule(
							info.FieldType,
							(module, requiredModule) => info.SetValue(module, requiredModule)
						),
					System.Reflection.PropertyInfo info
						=> new RequiredModule(
							info.PropertyType,
							(module, requiredModule) => info.SetValue(module, requiredModule)
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
		
		foreach(var r in requiredModules)
			_requiredBy.GetOrAdd(r.ModuleType).Add(type);

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

	#endregion

	#region RemoveModule

	/// <summary>
	/// Removes <em>all</em> modules of the specified <typeparamref name="T"/> type from the agent
	/// and calls <see cref="IDisposable.Dispose"/> on those that implements <see cref="IDisposable"/>.
	/// </summary>
	/// <typeparam name="T">
	/// 	The module type.
	/// 	This must inherit from <see cref="Module"/>.
	/// </typeparam>
	/// <returns>
	/// 	True if at least one module of the given type was found and removed,
	/// 	False otherwise.
	/// </returns>
	public bool RemoveModule<T>()
	where T : Module
		=> RemoveModule(_modules.Where(c => c is T));

	/// <summary>
	/// Removes <em>all</em> modules of the specified type from the agent
	/// and calls <see cref="IDisposable.Dispose"/> on those that implements <see cref="IDisposable"/>.
	/// </summary>
	/// <param name="moduleType">
	/// 	The module type.
	/// 	This must inherit from <see cref="Module"/>
	/// </param>
	/// <returns>
	/// 	True if at least one module of the given type was found and removed,
	/// 	False otherwise.
	/// </returns>
	public bool RemoveModule(Type moduleType)
		=> RemoveModule(_modules.Where(c => c.GetType() == moduleType));

	/// <summary>
	/// Removes the specified modules from the agent
	/// and calls <see cref="IDisposable.Dispose"/> on each one that implements <see cref="IDisposable"/>.
	/// </summary>
	/// <param name="modules">The modules to remove.</param>
	/// <returns>True if at least one module was found and successfully removed, False otherwise.</returns>
	public bool RemoveModule(params IEnumerable<Module> modules)
	{
		if(State is Initializing)
			throw new InvalidOperationException(
				"Cannot remove modules from an agent while it's initializing."
			);

		if(
			State is Disposed or Disposing
			|| modules is null
			|| modules
				?.Where(m => m.Agent == this || _modules.Contains(m))
				.ToListIfNotEmpty()
				is not { } toBeRemoved
		)
		{
			return false;
		}

		// Sort modules by dependency order
		// but cancel if any module is required by another module that isn't being removed.
		Dictionary<Module, List<Module>>? dependentsPerModule = null;
		foreach(var module in toBeRemoved)
			if(GetModulesThatRequire(module.GetType()).ToListIfNotEmpty() is { } dependents)
			{
				// if any module that depends on this one
				// is not in the list of modules to be removed...
				if(dependents.Any(d => !toBeRemoved.Contains(d)))
				{
					// ...then we cannot remove any of the modules
					Debug.LogWarning(
						"Cannot remove Module {module} from Agent {Name}"
						+ "because it is required by {dependents}.",
						module,
						this,
						dependents.JoinVerbose()
					);
					return false;
				}
				(dependentsPerModule ??= [])[module] = dependents;
			}
		// if any dependents were found, sort the modules so that dependents come first
		if(dependentsPerModule is not null)
			toBeRemoved.Sort((a, b) => dependentsPerModule[a].Contains(b) ? 1 : -1);

		// setup error aggregation
		using var errors = new ExceptionAggregator<string>(errorList =>
			$@"Failed to dispose of {
				errorList.Count
			} modules on agent: {
				Name
			}: {
				errorList
					.Select(x => x.value)
					.JoinVerbose()
			}"
		);

		// dispose of the modules
		for(int i = 0; i < toBeRemoved.Count; i++)
		{
			var module = toBeRemoved[i];
			try
			{
				if(module is IDisposable disposable)
					disposable.Dispose();
			}
			catch(Exception error) // if removal fails
			{
				if(error is InvalidOperationException ex)
					// We treat InvalidOperationException as a warning
					// to allow modules to cancel their own removal.
					Debug.LogWarning(
						"Module {module} cancelled removal from agent {agent}: {reason}",
						module,
						this,
						ex.Message
					);
				else
					// We treat all other exceptions as errors to be rethrown.
					errors.Add(error, module.ToString());

				// cancel the removal of this module
				toBeRemoved.RemoveAt(i--);

				// cancel the removal of subsequent modules that this one depends on
				if(dependentsPerModule is not null)
					foreach(var (requirement, requiredBy) in dependentsPerModule)
						if(requiredBy.Contains(module)
						&& toBeRemoved.IndexOf(requirement, i) is int index && index >= i)
							toBeRemoved.RemoveAt(index);
			}
		}
		
		// remove the modules from the agent that were successfully disposed of
		if(toBeRemoved.Count > 0)
		{
			foreach(var module in toBeRemoved)
				module.Agent = null!;
			_modules.RemoveAll(toBeRemoved.Contains);
			_updateRunningModules?.TrySetResult(); // cancel RunAsync (if running)
			return true;
		}
		return false;
	}

	#endregion

	#region GetModule

	/// <summary>
	/// Gets the last module that can be assigned
	/// to a variable of the specified <typeparamref name="T"/> type
	/// or <see langword="null"/> if the agent does not contain such a module.
	/// Modules added later take precedence over modules added before them
	/// which is why the last module that meets the criteria is returned.
	/// </summary>
	/// <returns>
	/// 	The most significant module assignable to the given type
	/// 	or <see langword="null"/> if not found.
	/// </returns>
	public T? GetModuleOrDefault<T>()
		where T : Module
		=> _modules.FindLast(m => m is T) as T;

	/// <summary>
	/// Gets the last module that can be assigned
	/// to a variable of the specified <paramref name="moduleType"/>
	/// or <see langword="null"/> if the agent does not contain such a module.
	/// Modules added later take precedence over modules added before them
	/// which is why the last module that meets the criteria is returned.
	/// </summary>
	/// <returns>
	/// 	The most significant module assignable to the given type
	/// 	or <see langword="null"/> if not found.
	/// </returns>
	public Module? GetModuleOrDefault(Type moduleType)
		=> _modules.FindLast(m => m.GetType().IsAssignableTo(moduleType));

	/// <summary>
	/// Gets the last module that can be assigned
	/// to a variable of the specified <typeparamref name="T"/> type
	/// or throws an <see cref="InvalidOperationException"/>
	/// if the agent does not contain such a module.
	/// Modules added later take precedence over modules added before them
	/// which is why the last module that meets the criteria is returned.
	/// </summary>
	/// <returns>
	/// 	The most significant module assignable to the given type.
	/// </returns>
	public T GetModule<T>()
		where T : Module
		=> GetModuleOrDefault<T>()
		?? throw new Exception(
			$"Module of type {typeof(T).ToVerboseString()} not found."
		);

	/// <summary>
	/// Gets last module that can be assigned
	/// to a variable of the specified <paramref name="moduleType"/>
	/// or throws an <see cref="InvalidOperationException"/>
	/// if the agent does not contain such a module.
	/// Modules added later take precedence over modules added before them
	/// which is why the last module that meets the criteria is returned.
	/// </summary>
	/// <returns>
	/// 	The most significant module assignable to the given type.
	/// </returns>
	public Module GetModule(Type moduleType)
		=> GetModuleOrDefault(moduleType)
		?? throw new Exception(
			$"Module of type {moduleType.ToVerboseString()} not found."
		);

	/// <summary>
	/// Gets all modules that can be assigned
	/// to a variable of the specified <typeparamref name="T"/> type.
	/// Modules are returned in the reverse order they were added to the agent.
	/// Modules added later take precedence over modules added before them
	/// which is why the modules are returned in reverse order.
	/// </summary>
	/// <returns>
	/// 	All modules that can be assigned to the given type
	/// 	or an empty enumerable if none were found.
	/// </returns>
	public IEnumerable<T> GetModules<T>()
		where T : Module
		=> _modules.OfType<T>().Reverse();
	
	/// <summary>
	/// Gets all modules on the agent that requires a module of type <typeparamref name="T"/>.
	/// <br/>See <see cref="RequiredModuleAttribute"/> for more information.
	/// </summary>
	public IEnumerable<Module> GetModulesThatRequire<T>()
		where T : Module
		=> GetModulesThatRequire(typeof(T));

	/// <summary>
	/// Gets all modules on the agent that requires a module of the specified <paramref name="moduleType"/>.
	/// <br/>See <see cref="RequiredModuleAttribute"/> for more information.
	/// </summary>
	public IEnumerable<Module> GetModulesThatRequire(Type moduleType)
		=> _requiredBy
			.GetValueOrDefault(moduleType)
			?.SelectMany(r => _modules.Where(m => m.GetType() == r))
			?? [];

	/// <summary>
	/// Gets all modules on the agent that requires a module of type <typeparamref name="T"/>
	/// and all modules that require those modules, etc.
	/// <br/>See <see cref="RequiredModuleAttribute"/> for more information.
	/// </summary>
	public IEnumerable<Module> GetModulesRecursivelyThatRequire<T>()
		where T : Module
		=> GetModulesRecursivelyThatRequire(typeof(T), []);

	/// <summary>
	/// Gets all modules on the agent that requires a module of the specified <paramref name="moduleType"/>
	/// and all modules that require those modules, etc.
	/// <br/>See <see cref="RequiredModuleAttribute"/> for more information.
	/// </summary>
	public IEnumerable<Module> GetModulesRecursivelyThatRequire(Type moduleType)
		=> GetModulesRecursivelyThatRequire(moduleType, []);
	
	private HashSet<Module> GetModulesRecursivelyThatRequire(Type moduleType, HashSet<Module> results)
	{
		if(_requiredBy.TryGetValue(moduleType, out var dependents))
			foreach(var dependentType in dependents)
				if(_modules.Find(m => m.GetType() == dependentType) is Module dependent
				&& results.Add(dependent))
					GetModulesRecursivelyThatRequire(dependentType, results);
		return results;
	}

	#endregion

	#region GetOrAddModule

	/// <summary>
	/// Gets the first module of the specified <typeparamref name="T"/> type
	/// or adds a new module of that type to the agent if it does not contain one yet.
	/// </summary>
	/// <param name="configure">
	/// 	(optional) callback to configure the module before it is initialized.
	/// </param>
	/// <param name="createModule">A function that creates the module.</param>
	/// <param name="type">The module type.</param>
	/// <typeparam name="T">The module type.</typeparam>
	/// <returns>The module of the specified type.</returns>
	public T GetOrAddModule<T>(Action<T>? configure = null)
		where T : Module
		=> GetModuleOrDefault<T>()
		?? (T)AddModuleInternal(
			typeof(T),
			configure is null
				? null
				: m => configure((T)m)
		);
	
	/// <inheritdoc cref="GetOrAddModule{T}(Action{T}?)"/>
	public T GetOrAddModule<T>(Func<Agent, T> createModule)
		where T : Module
		=> GetModuleOrDefault<T>()
		?? (T)AddModuleInternal(
			typeof(T),
			null,
			createModule
		);
	
	/// <inheritdoc cref="GetOrAddModule{T}(Action{T}?)"/>
	public Module GetOrAddModule(Type moduleType, Action<Module>? configure = null)
		=> GetModuleOrDefault(moduleType)
		?? AddModuleInternal(moduleType, configure);

	/// <inheritdoc cref="GetOrAddModule{T}(Action{T}?)"/>
	public Module GetOrAddModule(Type moduleType, Func<Agent, Module> createModule)
		=> GetModuleOrDefault(moduleType)
		?? AddModuleInternal(moduleType, null, createModule);

	#endregion

	#region Initialize
	/// <summary>
	/// Initializes all modules on the agent in the order they were added.
	/// <para>
	/// 	This method calls
	/// 	<see cref="Module.Initialize"/> / <see cref="Module.InitializeAsync"/>
	/// 	on each module.
	/// 	If both methods are overridden in a module, the async method takes precedence.
	/// </para>
	/// <para>
	/// 	You cannot dispose of the agent while it is initializing.
	/// </para>
	/// </summary>
	/// <exception cref="InvalidOperationException">
	/// 	Thrown when the agent has already been disposed of
	/// 	or if any of the modules attempt to dispose of the agent.
	/// </exception>
	/// <exception cref="AggregateException">
	/// 	Thrown when one or more modules fail to initialize.
	/// </exception>
	/// <param name="synchronous">
	/// 	Run the initialization synchronously,
	/// 	blocking the current thread
	/// 	until initialization is complete.
	/// </param>
	/// <returns>
	/// 	The fully <see cref="Initialized"/> agent
	/// 	if <paramref name="synchronous"/> = <see langword="true"/>
	/// 	or if all modules initialized synchronously.
	///
	/// 	Otherwise, the agent is returned immediately
	/// 	while still <see cref="Initializing"/> in the background.
	/// </returns>
	public Agent Initialize(bool synchronous = false)
	{
		var init = InitializeAsync();
		if(synchronous)
			init.Wait();
		else
			Task.Run(() => init);
		return this;
	}
	/// <returns>
	/// 	A task that completes when all modules have been initialized
	/// 	and the agent is in the <see cref="Initialized"/> state.
	/// </returns>
	/// <remarks>
	/// 	If you choose to override this method in a derived class,
	/// 	make sure to call and await the base method.
	/// </remarks>
	/// <inheritdoc cref="Initialize"/>
	public virtual async Task<Agent> InitializeAsync()
	{
		if(State is Disposed or Disposing)
			throw new ObjectDisposedException($"Cannot initialize an already {State} agent.");
		if(State is Initialized or Initializing)
			return this;

		var log = Debug.BeginScopeValues(("Agent", Name));
		Debug.LogInformation("Initializing {Name}", Name);
		State = Initializing;
		try
		{
			using var errors = new ExceptionAggregator<string>(errorList =>
				$@"Failed to initialize {
					errorList.Count
				} modules on agent: {
					Name
				}: {
					errorList
						.Select(x => x.value)
						.JoinVerbose()
				}"
			);
			foreach(var module in _modules.ToList())
				try
				{
					await module.InitializeAsync();
				}
				catch(Exception error)
				{
					errors.Add(error, module.ToString());
				}
			Debug.LogInformation("{Name} initialized successfully", Name);
		}
		catch
		{
			Debug.LogError("Failed to initialize {Name}", Name);
			throw;
		}
		finally
		{
			State = Initialized;
			log?.Dispose();
		}
		return this;
	}

	#endregion

	#region Run

	private CancellationTokenSource? _runCancellation; // used to cancel the agent's RunAsync method
	private TaskCompletionSource? _updateRunningModules; // used to update the list of running modules

	/// <summary>
	/// Runs the agent by calling every module's
	/// <see cref="Module.RunAsync">RunAsync()</see>
	/// method in parallel.
	/// <list type="bullet">
	/// 	<item>
	///			Adding or removing modules while the agent is running
	///			will automatically update the list of running modules.
	///		</item>
	///		<item>
	///			Only when all modules have finished running
	///			will the agent return control to the caller.
	///		</item>
	///		<item>
	///			Exceptions thrown by modules are caught and handled
	///			according to the <paramref name="exceptionPolicy"/> parameter.
	///		</item>
	///	</list>
	/// </summary>
	/// <param name="exceptionBehavior">
	/// 	Defines how the agent should behave
	/// 	when an exception occurs in a module's
	/// 	<see cref="Module.RunAsync">RunAsync()</see> method.
	/// </param>
	public async Task RunAsync(
		RunningModuleExceptionPolicy exceptionPolicy = RunningModuleExceptionPolicy.LogAndStopModule,
		CancellationToken cancellationToken = default
	)
	{
		if(State is Disposed or Disposing)
			throw new ObjectDisposedException("Disposed agents cannot run.");
		
		if(State is Uninitialized)
			throw new InvalidOperationException("Cannot run an uninitialized agent.");
		
		if(State is Initializing)
			throw new InvalidOperationException("Cannot run an agent while it is initializing.");

		if(_runCancellation is not null)
			throw new InvalidOperationException("The agent is already running.");
		
		var currentlyRunning = new List<ModuleRun>();
		while(true)
		{
			_updateRunningModules = new();
			_runCancellation = new();
			using var cancel = _runCancellation;
			using var _ = cancellationToken.Register(cancel.Cancel);
			try
			{
				// start running modules that are not already running
				foreach(var module in _modules)
					if(!currentlyRunning.Any(t => t.Module == module))
					{
						var cancelModule = new CancellationTokenSource();
						cancel.Token.Register(cancelModule.Cancel); // no need to dispose of this CancellationTokenRegistration
						currentlyRunning.Add(new ModuleRun(
							module,
							RunModule(module, exceptionPolicy, cancelModule.Token),
							cancelModule
						));
					}

				// stop running modules that are no longer part of the agent
				var toRemove = currentlyRunning
					.Where(t => !_modules.Contains(t.Module))
					.ToList();
				foreach(var run in toRemove)
				{
					run.Cancel.Cancel();
					currentlyRunning.Remove(run);
				}

				// if _modules have changed...
				if(_updateRunningModules.Task.IsCompleted)
					// we need to rerun the loop
					// to update the list of running modules
					continue;
				
				await Task.WhenAny(
					// wait for either all modules to finish...
					Task.WhenAll(currentlyRunning.Select(t => t.Task)),
					// ...or for the list of modules to change
					_updateRunningModules.Task
				);

				// if _modules have changed...
				if(_updateRunningModules.Task.IsCompleted)
					// we need to rerun the loop
					// to update the list of running modules
					continue;
				else
					// otherwise, all modules have finished running
					return;

			}
			catch
			{
				if(cancel.IsCancellationRequested)
					return;
				if(exceptionPolicy is RunningModuleExceptionPolicy.RethrowAndStopAll)
					throw;

				cancel.Cancel();
				currentlyRunning.Clear();
				if(exceptionPolicy is RunningModuleExceptionPolicy.LogAndRerunAll)
				{
					Debug.LogInformation("Rerunning agent {name}.", Name);
					await Task.Delay(10, cancel.Token);
				}
				else
				{
					if(exceptionPolicy is RunningModuleExceptionPolicy.LogAndStopAll)
						Debug.LogInformation("Stopping agent {name}.", Name);
					return;
				}
			}
			finally
			{
				_runCancellation = null;
				_updateRunningModules = null;
			}
		}
	}

	private Task RunModule(Module module, RunningModuleExceptionPolicy exceptionBehavior, CancellationToken cancellationToken)
		=> Task.Run(async () =>
		{
			while(true)
			{
				try
				{
					await module.RunAsync(cancellationToken);
					return;
				}
				catch(Exception error)
				{
					if(cancellationToken.IsCancellationRequested)
					{
						if(error is OperationCanceledException)
							throw;
						throw new OperationCanceledException();
					}

					if(exceptionBehavior is RunningModuleExceptionPolicy.RethrowAndStopAll)
						throw;

					Debug.LogError(error, "An error occurred while running {module} on agent {name}.", module, Name);
					switch(exceptionBehavior)
					{
						case RunningModuleExceptionPolicy.LogAndStopModule:
							Debug.LogInformation("Stopping module {module} from agent {name}.", module, Name);
							await Task.Delay(10, cancellationToken);
							return;
						case RunningModuleExceptionPolicy.LogAndRemoveModule:
							Debug.LogInformation("Removing module {module} from agent {name}.", module, Name);
							await Task.Delay(10, cancellationToken);
							RemoveModule(module);
							return;
						case RunningModuleExceptionPolicy.LogAndRerunModule:
							Debug.LogInformation("Rerunning module {module} on agent {name}.", module, Name);
							await Task.Delay(10, cancellationToken);
							break;
						default:
							await Task.Delay(10, cancellationToken);
							throw;
					}
				}
			}
		}, cancellationToken);

	public void Stop()
		=> _runCancellation?.Cancel();

	public bool IsRunning
		=> _runCancellation is not null;

	/// <summary>
	/// Container for a running module,
	/// the task representing the module's RunAsync method,
	/// and the cancellation token source to cancel it
	/// when the module is removed from the agent.
	/// </summary>
	/// <param name="Module"></param>
	/// <param name="Task"></param>
	/// <param name="CancelModule"></param>
	private record ModuleRun
	(
		Module Module,
		Task Task,
		CancellationTokenSource Cancel
	);

	#endregion

	#region Dispose

	/// <summary>
	/// Disposes of the agent and all of its modules.<br/>
	/// All modules that implement <see cref="IDisposable"/>
	/// will have their <see cref="IDisposable.Dispose"/> method called.
	/// </summary>
	/// <remarks>
	/// 	If you choose to override this method in a derived class,
	/// 	make sure to call the base method,
	/// 	preferably at the start of the method.
	/// </remarks>
	public virtual void Dispose()
	{
		if(State is Disposed or Disposing)
			return;
		
		if(State is Initializing)
			throw new InvalidOperationException(
				"Cannot dispose of an agent while it is initializing."
			);

		_runCancellation?.Cancel();

		var log = Debug.BeginScopeValues(("Agent", Name));
		Debug.LogInformation("Disposing of {Name}", Name);
		State = Disposing;
		try
		{
			using var errors = new ExceptionAggregator<string>(errorList =>
				$@"Failed to dispose of {
						errorList.Count
					} modules on Agent {
						Name
					}: {
						errorList
							.Select(x => x.value)
							.JoinVerbose()
					}"
			);

			// dispose of the modules in reverse order
			var modulesReversed = new List<Module>(_modules.Reverse<Module>());
			foreach(var module in modulesReversed)
				module._signalers.Clear(); // avoid redundant cleanup
			foreach(var module in modulesReversed)
				if(module is IDisposable disposable)
					try
					{
						disposable.Dispose();
					}
					catch(Exception error)
					{
						if(error is not InvalidOperationException) // ignore cancellation
							errors.Add(error, module.ToString());
					}
			foreach(var module in modulesReversed)
				module.Agent = null!;
			_signalersByType.Clear();
			_modules.Clear();
			if(!errors.HasErrors)
				Debug.LogInformation("{Name} disposed successfully", Name);
		}
		catch
		{
			Debug.LogError("Failed to dispose of {Name}", Name);
			throw;
		}
		finally
		{
			State = Disposed;
			_state.Dispose();
			log?.Dispose();
			GC.SuppressFinalize(this);
		}
	}

	#endregion

	#region Signals

	private readonly ConcurrentDictionary<Type, Signaler> _signalersByType = [];
	internal Signaler<TSignal> GetSignaler<TSignal>()
	{
		if(State is Disposed or Disposing)
			throw new ObjectDisposedException("Disposed agents cannot send signals.");
		
		return (Signaler<TSignal>)
			_signalersByType.GetOrAdd(
				typeof(TSignal),
				_ => new Signaler<TSignal>()
			);
	}

	/// <summary>
	/// Returns an <see cref="IObservable{TSignal}"/>
	/// that emits all future signals of type <typeparamref name="TSignal"/>
	/// that are sent on the agent.
	/// <br/>
	/// Example:
	/// <code language="csharp">
	/// 	var myAgent = new Agent()
	/// 		.AddModule&lt;MyModule&gt;()
	/// 		.Initialize();
	/// 	myAgent
	/// 		.ObserveSignals&lt;MySignal&gt;()
	/// 		.Subscribe(signal => Console.WriteLine($"Received signal: {signal}"));
	/// 	myAgent.SendSignal(new MySignal());
	/// </code>
	/// <br/>
	/// Use the <seealso cref="ObserveSignals{TSignal}(out IObservable{TSignal})"/>
	/// method instead when configuring the agent in a method chain.
	/// </summary>
	/// <returns>The observable that emits specified signal type.</returns>
	public IObservable<TSignal> ObserveSignals<TSignal>()
	{
		if(State is Disposed or Disposing)
			throw new ObjectDisposedException("Disposed agents cannot observe signals.");
		
		return GetSignaler<TSignal>();
	}

	/// <summary>
	/// Retrieves an <see cref="IObservable{TSignal}"/>
	/// that emits all future signals of type <typeparamref name="TSignal"/>
	/// that are sent on the agent.
	/// <br/>
	/// Use this method when configuring the agent in a method chain. Example:
	/// <code language="csharp">
	/// 	var myAgent = new Agent()
	/// 		.AddModule&lt;MyModule&gt;()
	/// 		.ObserveSignals&lt;MySignal&gt;(out var mySignals)
	/// 		.Initialize();
	/// 	mySignals.Subscribe(signal => Console.WriteLine($"Received signal: {signal}"));
	/// 	myAgent.SendSignal(new MySignal());
	/// </code>
	/// <br/>
	/// When not configuring the agent it's recommended to instead
	/// use the <seealso cref="ObserveSignals{TSignal}()"/> method.
	/// </summary>
	/// <param name="observable">A reference to assign the observable to.</param>
	/// <returns>The agent for further configuration.</returns>
	public Agent ObserveSignals<TSignal>(out IObservable<TSignal> observable)
	{
		observable = GetSignaler<TSignal>();
		return this;
	}

	/// <summary>
	/// Sends a <typeparamref name="TSignal"/> signal to all modules and observers on the agent.
	/// <para>
	/// 	This method is non-blocking and returns immediately
	/// 	because it defers the signal processing to a background task.
	/// </para>
	/// <para>
	/// 	It's perfectly fine to add and remove modules during signal processing,
	/// 	but be aware that modules that are removed will not receive the signal
	/// 	and modules that are added will only receive future signals, i.e. sent after they were added.
	/// </para>
	/// </summary>
	/// <param name="signal">The signal to send.</param>
	/// <param name="synchronous">Whether to wait for all the responses before returning.</param>
	/// <typeparam name="TSignal">The type of signal to send.</typeparam>
	/// <typeparam name="TResponse">The type of response to expect.</typeparam>
	/// <returns>
	/// 	All responses to the signal of type <typeparamref name="TResponse"/>.
	/// 	<see cref="Enumerable"/> offers a variety of methods to work with the responses.
	/// </returns>
	public IEnumerable<TResponse> SendSignal<TSignal, TResponse>(TSignal signal, bool synchronous = false)
	{
		if(State is Disposed)
			throw new ObjectDisposedException("Disposed agents cannot send signals.");
		var response = SendSignalAsync<TSignal, TResponse>(signal);
		if(State is Disposing
		|| synchronous)
			return response.ToBlockingEnumerable(); // when disposing, wait for the response
		
		Task.Run(() => response);
		return [];
	}


	/// <summary>
	/// Sends a signal asynchronously to all modules on the agent and observers.
	/// <para>
	/// 	It's perfectly fine to add and remove modules during signal processing,
	/// 	but be aware that modules that are removed will not receive the signal
	/// 	and modules that are added will only receive future signals, i.e. sent after they were added.
	/// </para>
	/// </summary>
	/// <seealso cref="SendSignal{TSignal}"/>
	/// <param name="signal">The signal to send.</param>
	/// <typeparam name="TSignal">The type of signal to send.</typeparam>
	/// <returns>
	/// 	All responses to the signal of type <typeparamref name="TResponse"/>.
	/// 	<see cref="XAsyncEnumerable"/> offers a variety of methods to work with the responses.
	/// </returns>
	public IAsyncEnumerable<TResponse> SendSignalAsync<TSignal, TResponse>(TSignal signal)
	{
		if(State is Disposed)
			throw new ObjectDisposedException("Disposed agents cannot send signals.");

		Debug.LogTrace(
			"Sending signal on Agent {name}: {signal}",
			Name, signal
		);

		if(_signalersByType.TryGetValue(typeof(TSignal), out var signaler))
			return ((Signaler<TSignal>)signaler)
				.SendSignalAsync<TResponse>(signal);
		
		return AsyncEnumerable.Empty<TResponse>();
	}

	/// <inheritdoc cref="SendSignal{TSignal, TResponse}(TSignal, bool)"/>
	public void SendSignal<TSignal>(TSignal signal)
		=> SendSignal<TSignal, object>(signal);

	/// <returns>
	/// 	A task that completes when all modules have processed the signal.
	/// </returns>
	/// <inheritdoc cref="SendSignalAsync{TSignal, TResponse}(TSignal)"/>
	public Task SendSignalAsync<TSignal>(TSignal signal)
	{
		if(State is Disposed)
			throw new ObjectDisposedException("Disposed agents cannot send signals.");
		return SendSignalAsync<TSignal, object>(signal)
			.ToTask();
	}

	/// <summary>
	/// Specifies the order in which modules must process signals of the given type.
	/// <br/>
	/// Example:
	/// <code language="csharp">
	/// var myAgent = new Agent()
	/// 	.AddModule&lt;LastModule&gt;()
	/// 	.AddModule&lt;ThirdModule&gt;()
	/// 	.AddModule&lt;SecondModule&gt;()
	/// 	.AddModule&lt;FirstModule&gt;()
	/// 	.SetSignalProcessingOrder&lt;MySignal&gt;(a =>
	/// 	[
	/// 		a.GetModule&lt;FirstModule&gt;(),
	/// 		a.GetModule&lt;SecondModule&gt;(),
	/// 		a.GetModule&lt;ThirdModule&gt;(),
	/// 		a.GetModule&lt;LastModule&gt;(),
	/// 	])
	/// 	.Initialize();
	/// </code>
	/// </summary>
	/// <param name="getModuleOrder">A function that returns the order in which modules should process the signals.</param>
	public Agent SetSignalProcessingOrder<TSignal>(Func<Agent, Module[]> getModuleOrder)
	{
		if(State is Disposed)
			throw new ObjectDisposedException("Cannot set signal processing order on a disposed agent.");
		
		var moduleOrder = getModuleOrder(this) ?? throw new ArgumentNullException(nameof(getModuleOrder));

		_signalersByType
			.GetOrAdd(
				typeof(TSignal),
				_ => new Signaler<TSignal>()
			)
			.SetSignalProcessingOrder(moduleOrder);
		
		return this;
	}

	#endregion

	#region ToString

	/// <summary>
	/// Returns a single-line string representation of the agent and its modules.<br/>
	/// If you need a more verbose multiline string representation, use <see cref="ToStringPretty">ToStringPretty()</see> instead.
	/// <para>
	/// 	Unlike <see cref="ToStringPretty">ToStringPretty()</see>, this method
	/// 	lists each module's type instead of calling their <see cref="Module.ToString"/> method.
	/// </para>
	/// Example:
	/// <code language="csharp">
	/// 	Console.WriteLine(
	/// 		new Agent
	/// 		{
	/// 			Name = "foobar",
	/// 			Description = "A placeholder agent",
	/// 		}
	/// 		.AddModule&lt;FooModule&gt;()
	/// 		.AddModule&lt;BarModule&gt;()
	/// 		.Initialize()
	/// 		.ToString()
	/// 	);
	/// 	public class FooModule : Module { }
	/// 	public class BarModule : Module { }
	/// 	// Output:
	/// 	// Agent foobar { Foo, Bar }
	/// </code>
	/// </summary>
	/// <returns>
	/// 	A string in the format of
	/// 	"Agent <see cref="Name"/> { <see cref="Modules"/>  }"
	/// </returns>
	public override string ToString() =>
		$@"Agent {Name?.NullIfWhiteSpace()
				?.WithMaxLengthSuffixed(20)
				?.Thru(x => x + ' ')
			?? ""}{State switch
			{
				Initialized => "",
				Uninitialized => "(uninitialized) ",
				Initializing => "(initializing) ",
				Disposed => "(disposed) ",
				Disposing => "(disposing) ",
				_ => State + " "
			}}{{ {_modules
				.Select(x => x.TypeName.WithMaxLengthSuffixed(20))
				.Join()} }}";

	/// <summary>
	/// Returns a multiline string representation of the agent and its modules.
	/// <para>
	/// 	Unlike <see cref="ToString"/>
	/// 	this method includes the <see cref="Description"/>
	/// 	and lists the results of calling
	/// 	each module's <see cref="Module.ToString"/>.
	/// </para>
	/// Examples:
	/// <br/>
	/// Agent <see cref="Description"/>
	/// fits on one line (does not exceed <paramref name="maxWidth"/>).
	/// <code language="csharp">
	/// 	Console.WriteLine(
	/// 		new Agent
	/// 		{
	/// 			Name = "foobar",
	/// 			Description = "A placeholder agent",
	/// 		}
	/// 		.AddModule&lt;FooModule&gt;()
	/// 		.AddModule&lt;BarModule&gt;()
	/// 		.Initialize()
	/// 		.ToStringPretty()
	/// 	);
	/// 	public class FooModule : Module { }
	/// 	public class BarModule : Module { }
	/// 	// Output:
	/// 	// Agent foobar
	/// 	// ╭─────────────────────╮
	/// 	// │ A placeholder agent │
	/// 	// ╰─────────────────────╯
	/// 	//   ├─ Foo
	/// 	//   └─ Bar
	/// </code>
	/// Agent <see cref="Description"/>
	/// and <c>Foo.ToString()</c> gets wrapped
	/// (length exceeds <paramref name="maxWidth"/>).
	/// <code language="csharp">
	/// 	Console.WriteLine(
	/// 		new Agent
	/// 		{
	/// 			Name = "foobar",
	/// 			Description = "A placeholder agent with a seriously long description that needs to be wrapped around",
	/// 		}
	/// 		.AddModule&lt;FooModule&gt;()
	/// 		.AddModule&lt;BarModule&gt;()
	/// 		.Initialize()
	/// 		.ToStringPretty()
	/// 	);
	/// 	public class FooModule : Module
	/// 	{
	/// 		public override string ToString() => "Foo with a long description that needs to be wrapped around";
	/// 	}
	/// 	public class BarModule : Module { }
	/// 	// Output:
	/// 	// Agent foobar
	/// 	// ╭─────────────────────────────────────────────╮
	/// 	// │ A placeholder agent with a seriously long   │
	/// 	// │ description that needs to be wrapped around │
	/// 	// ╰─────────────────────────────────────────────╯
	/// 	//   ├─ Foo with a long description that needs to be
	/// 	//   │  wrapped around
	/// 	//   └─ Bar
	/// </code>
	/// </summary>
	/// <param name="maxWidth">
	/// 	The maximum width of each line in the output.
	/// </param>
	/// <returns>
	/// 	A string representation of the agent which includes
	/// 	the <see cref="Name" />, <see cref="Description"/>
	/// 	and <see cref="Modules"/>.
	/// </returns>
	public virtual string ToStringPretty(int maxWidth = 50)
	{
		maxWidth = maxWidth.OrAtLeast(10);
		var name = $@"{
			(
				string.IsNullOrWhiteSpace(Name)
					? "Anonymous Agent"
					: "Agent " + Name
			)}{
				State switch
				{
					Initialized => "",
					Uninitialized => " (uninitialized)",
					Initializing => " (initializing)",
					Disposed => " (disposed)",
					Disposing => " (disposing)",
					_ => " " + State
				}
			}"
			.WithMaxLengthSuffixed(maxWidth);

		var description = string.IsNullOrWhiteSpace(Description)
			? ""
			: $"\n{Description.WrapInsideBox(maxWidth)}";

		return $@"{name}{description}{_modules.Count switch { 0 => "", 1 => "\n  └─ ", _ => "\n  ├─ " }}{_modules
				.Select(x
					=> x.ToString()
					?? x.GetType().Name
				)
				.ToList()
				.Tap(list =>
				{
					for(var i = 0; i < list.Count; i++)
						list[i] = list[i]
							.SplitAndWrapLines(maxWidth - 5)
							.Join(i < list.Count - 1
								? "\n  │  "
								: "\n     "
							);
				})
				.JoinVerbose(
					"\n  ├─ ",
					"\n  └─ "
				)}";
	}

	#endregion
}
