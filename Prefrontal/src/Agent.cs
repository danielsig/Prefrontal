using System.Collections.Concurrent;
using static Prefrontal.AgentState;

namespace Prefrontal;

/// <summary>
/// An agent manages a collection of <see cref="Module"/>s that work together to achieve a goal.
/// <list type="bullet">
/// 	<item>To create an agent simply instantiate it with the <see langword="new"/> operator.</item>
/// 	<item>Add modules to the agent by chaining <see cref="AddModule{T}(Action{T}?)"/> method calls.</item>
/// 	<item>Finally call <see cref="Initialize"/> to initialize all modules and start the agent.</item>
/// 	<item>When the agent is no longer needed, call <see cref="IDisposable.Dispose"/> to dispose of it.</item>
/// </list>
/// <para>
/// 	If you need to dispose of the <see cref="ServiceProvider"/> when the agent is disposed
/// 	(e.g. if the ServiceProvider was specifically created for that agent),
/// 	simply add <see cref="ServiceProviderDisposesWithAgentModule"/> to the agent.
/// </para>
/// </summary>
public class Agent : IDisposable
{
	/// <summary>
	/// Creates a new agent without a <see cref="ServiceProvider"/>.
	/// </summary>
	public Agent()
	{
		ServiceProvider = EmptyServiceProvider.Instance;
		Debug = DefaultLogger;
	}

	/// <summary>
	/// Creates a new agent with the specified <paramref name="serviceProvider"/>.
	/// </summary>
	/// <param name="serviceProvider"></param>
	/// <exception cref="ArgumentNullException"></exception>
	/// <exception cref="InvalidOperationException"></exception>
	public Agent(IServiceProvider serviceProvider)
	{
		ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		Debug = serviceProvider.GetService<ILogger<Agent>>()
			?? serviceProvider.GetService<ILogger>()
			?? serviceProvider.GetService<ILoggerProvider>()?.CreateLogger("Agent")
			?? DefaultLogger;
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
		= new ServiceCollection()
			.AddLogging(builder => builder.AddConsole())
			.BuildServiceProvider()
			.UseFor(s => s.GetService<ILoggerProvider>()?.CreateLogger("Agent"))
			?? throw new InvalidOperationException("No logger found.");

    
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
	/// Calling <see cref="Initialize"/> will set the state to <see cref="Initializing"/>
	/// while the modules are being initialized and to <see cref="Initialized"/> when they are done.
	/// Calling <see cref="Dispose"/> will set the state to <see cref="Disposing"/>
	/// while the modules are being disposed of and to <see cref="Disposed"/> when done.
	/// </summary>
	public AgentState State { get; private set; } = Uninitialized;

	#endregion
	
	#region AddModule

	/// <summary>
	/// Adds a module of the specified type to the agent.
	/// The module type's constructor gets called and the required services are injected from the <see cref="ServiceProvider"/>.
	/// The module's constructor can also take the agent itself as a parameter and even other modules that it requires.
	/// The optional <paramref name="configure"/> action can be used to configure the module before it is initialized.
	/// Don't forget to call <see cref="Initialize"/>
	/// on the agent after you've added all the modules.
	/// <para>
	/// 	When a module is added <em>after</em> the agent is initialized
	/// 	its <see cref="Module.Initialize"/> method is called immediately.
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
	/// Don't forget to call <see cref="Initialize"/>
	/// on the agent after you've added all the modules.
	/// <para>
	/// 	When a module is added <em>after</em> the agent is initialized
	/// 	its <see cref="Module.Initialize"/> method is called immediately.
	/// </para>
	/// </summary>
	/// <typeparam name="T">
	/// 	The module type.
	/// 	This must inherit from <see cref="Prefrontal.Module"/>
	/// </typeparam>
	/// <param name="createModule">
	/// 	A function that creates the module.
	/// </param>
	/// <returns>The current agent for further method chaining.</returns>
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
	/// <param name="type">
	/// 	The module type.
	/// 	This must inherit from <see cref="Prefrontal.Module"/>
	/// </param>
	public Agent AddModule(Type type, Action<Module>? configure = null)
	{
		AddModuleInternal(type, configure);
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
		if (typeData.IsSingleton
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
				module.Initialize();
			
			return module;
		}
		catch (Exception ex)
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
		if(State is Initializing)
			throw new InvalidOperationException(
				"Cannot remove modules from an agent while it's initializing."
			);
		
		if(type is null
		|| State is Disposed or Disposing
		|| _modules.Where(c => c.GetType() == type).ToList() is not List<Module> removed
		|| removed.Count == 0)
			return false;

		List<(string module, Exception error)>? errors = null;
		
		foreach(var module in removed)
			try
			{
				if(module is IDisposable disposable)
					disposable.Dispose();
			}
			catch(InvalidOperationException ex)
			{
				// handle when the module cancels the removal
				Debug.LogWarning(
					"Module {module} cancelled removal from agent {agent}: {reason}",
					module,
					this,
					ex.Message
				);
				removed.Remove(module);
			}
			catch(Exception error)
			{
				(errors ??= []).Add((module.ToString(), error));
			}
		if(removed.Count > 0)
		{
			foreach(var module in removed)
				module.Agent = null!;
			_modules.RemoveAll(removed.Contains);
			foreach(var list in SignalProcessorPriorityPerType.Values)
				list.RemoveAll(removed.Contains);
		}
		if(errors?.Count > 0)
			throw new AggregateException(
				$@"Failed to dispose of {
					errors.Count
				} modules on agent: {
					Name
				}: {
					errors
						.Select(x => x.module)
						.JoinVerbose()
				}",
				errors.Select(x => x.error)
			);
		return false;
	}

	/// <summary>
	/// Removes the specified module from the agent
	/// and calls <see cref="IDisposable.Dispose"/> on it if it implements <see cref="IDisposable"/>.
	/// </summary>
	/// <param name="module">The module to remove.</param>
	/// <returns>True if the module was found and removed, False otherwise.</returns>
	public bool RemoveModule(Module module)
	{
		if(State is Initializing)
			throw new InvalidOperationException(
				"Cannot remove modules from an agent while it's initializing."
			);
		
		if(module is null
		|| State is Disposed or Disposing
		|| _modules.IndexOf(module) is var index && index < 0)
			return false;
		
		try
		{
			if(module is IDisposable disposable)
				disposable.Dispose();
			return true;
		}
		catch(InvalidOperationException ex)
		{
			// handle when the module cancels the removal
			Debug.LogWarning(
				"Module {module} cancelled removal from agent {agent}: {reason}",
				module.GetType().ToVerboseString(),
				this,
				ex.Message
			);
			index = -1;
			return false;
		}
		finally
		{
			if(index >= 0) // if the module did not cancel the removal
			{
				_modules.RemoveAt(index);
				module.Agent = null!;
				foreach(var list in SignalProcessorPriorityPerType.Values)
					list.Remove(module);
			}
		}
	}

	#endregion

	#region GetModule

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
	public Module GetOrAddModule(Type type, Action<Module>? configure = null)
		=> GetModuleOrDefault(type)
		?? AddModuleInternal(type, configure);

	/// <inheritdoc cref="GetOrAddModule{T}(Action{T}?)"/>
	public Module GetOrAddModule(Type type, Func<Agent, Module> createModule)
		=> GetModuleOrDefault(type)
		?? AddModuleInternal(type, null, createModule);

	#endregion

	#region Overridable Methods
	/// <summary>
	/// Initializes all modules on the agent in the order they were added.
	/// You cannot dispose of the agent while it is initializing.
	/// </summary>
	/// <exception cref="InvalidOperationException">
	/// 	Thrown when the agent has already been disposed of
	/// 	or if any of the modules call attempt to dispose of the agent.
	/// </exception>
	/// <exception cref="AggregateException">
	/// 	Thrown when one or more modules fail to initialize.
	/// </exception>
	/// <returns>The current agent.</returns>
	public virtual Agent Initialize()
	{
		if(State is Disposed or Disposing)
			throw new ObjectDisposedException("Cannot initialize a disposed agent.");
		
		if(State is Initialized or Initializing)
			return this;
		
		using var _ = Debug.BeginScope("Initializing Agent {Name}", Name);
		State = Initializing;
		try
		{
			List<(string module, Exception error)>? errors = null;
			foreach(var module in _modules.ToList())
				try
				{
					module.Initialize();
				}
				catch(Exception error)
				{
					(errors ??= []).Add((module.ToString(), error));
				}
			if(errors?.Count > 0)
				throw new AggregateException(
					$@"Failed to initialize {
						errors.Count
					} modules on agent: {
						Name
					}: {
						errors
							.Select(x => x.module)
							.JoinVerbose()
					}",
					errors.Select(x => x.error)
				);
			return this;
		}
		finally
		{
			State = Initialized;
		}
	}

	/// <summary>
	/// Disposes of the agent and all of its modules.<br/>
	/// All modules that implement <see cref="IDisposable"/>
	/// will have their <see cref="IDisposable.Dispose"/> method called.
	/// </summary>
	public virtual void Dispose()
	{
		if(State is Disposed or Disposing)
			return;
		
		if(State is Initializing)
			throw new InvalidOperationException(
				"Cannot dispose of an agent while it is initializing."
			);

		using var _ = Debug.BeginScope("Disposing of Agent {Name}", Name);
		State = Disposing;
		try
		{
			List<(string module, Exception error)>? errors = null;

			// dispose of the modules in reverse order
			var modulesReversed = new List<Module>(_modules.Reverse<Module>());
			foreach(var module in modulesReversed)
			{
				if(module is IDisposable disposable)
					try
					{
						disposable.Dispose();
					}
					catch(Exception error)
					{
						if(error is not InvalidOperationException) // ignore cancellation
							(errors ??= []).Add((module.ToString(), error));
					}
				module.Agent = null!;
			}
			_modules.Clear();
			SignalProcessorPriorityPerType.Clear();
			if(errors?.Count > 0)
				throw new AggregateException(
					$@"Failed to dispose of {
						errors.Count
					} modules on agent: {
						Name
					}: {
						errors
							.Select(x => x.module)
							.JoinVerbose()
					}",
					errors.Select(x => x.error)
				);
		}
		finally
		{
			State = Disposed;
			GC.SuppressFinalize(this);
		}
	}

	public override string ToString()
		=> $@"{
			Name.NullIfWhiteSpace() ?? "Agent"
		} {(
			string.IsNullOrWhiteSpace(Description)
				? "" :
				$"({Description})"
		)}{
			_modules.Count switch { 0 => "", 1 => "\n  └─ ", _ => "\n  ├─ " }
		}{
			_modules.JoinVerbose("\n  ├─ ", "\n  └─ ")
		}";

	#endregion

	#region Signals

	internal readonly ConcurrentDictionary<Type, List<Module>> SignalProcessorPriorityPerType = [];

	/// <summary>
	/// Sends a signal asynchronously to all modules on the agent that implement either
	/// <see cref="ISignalReceiver{TSignal}"/>
	/// or
	/// <see cref="ISignalInterceptor{TSignal}"/>.
	/// </summary>
	/// <seealso cref="SendSignal{TSignal}"/>
	/// <param name="signal">The signal to send.</param>
	/// <typeparam name="TSignal">The type of signal to send.</typeparam>
	/// <returns>A task that represents the asynchronous operation.</returns>
	public async Task SendSignalAsync<TSignal>(TSignal signal)
	{
		if(State is Disposed or Disposing)
			throw new ObjectDisposedException("Disposed agents cannot send signals.");
		
		var order = SignalProcessorPriorityPerType.GetValueOrDefault(typeof(TSignal));
		var processors = _modules.OfType<ISignalProcessor<TSignal>>();
		if(order is not null)
			processors = processors.OrderBy(p => order.IndexOf((Module)p) switch { -1 => int.MaxValue, var i => i });
		
		var list = new List<ISignalProcessor<TSignal>>(processors);
		var index = 0;
		foreach(var processor in list)
			try
			{
				switch(processor)
				{
					case ISignalReceiver<TSignal> receiver:
						await receiver.ReceiveSignalAsync(signal);
						break;
					case ISignalInterceptor<TSignal> interceptor:
						if(await interceptor.InterceptSignalAsync(signal) is Intercepted<TSignal> intercepted)
						{
							if(intercepted.Result is SignalInterceptionResult.StopPropagation)
								return;
							signal = intercepted.Signal;
						}
						break;
					default:
						throw new InvalidOperationException(
							$"Signal processor {processor} not supported."
						);
				}
				++index;
			}
			catch(Exception ex)
			{
				Debug.LogError(
					ex,
					"Error processing signal {signal} by {processor}{at index}.",
					signal,
					processor,
					index > 0 ? " at index " + index : ""
				);
			}
	}

	/// <summary>
	/// Sends a signal to all modules on the agent that implement either
	/// <see cref="ISignalReceiver{TSignal}"/>
	/// or
	/// <see cref="ISignalInterceptor{TSignal}"/>.
	/// This method is non-blocking and returns immediately
	/// because it defers the signal processing to a background task.
	/// </summary>
	/// <param name="signal">The signal to send.</param>
	/// <typeparam name="TSignal">The type of signal to send.</typeparam>
	public void SendSignal<TSignal>(TSignal signal)
		=> Task.Run(() => SendSignalAsync(signal));

	/// <summary>
	/// Specifies the order in which modules must process signals of the given type.
	/// <br/>
	/// Example:
	/// <code>
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
	/// <seealso cref="ISignalInterceptor{TSignal}"/>
	/// <seealso cref="ISignalReceiver{TSignal}"/>
	public Agent SetSignalProcessingOrder<TSignal>(Func<Agent, List<Module>> getModuleOrder)
	{
		var moduleOrder = getModuleOrder(this);
		foreach(var module in moduleOrder)
		{
			if(module is not ISignalProcessor<TSignal>)
				throw new ArgumentException($"Module {module} does not implement {typeof(ISignalProcessor<TSignal>).ToVerboseString()}.");
			if(module.Agent != this)
				throw new ArgumentException($"Module {module} does not belong to the agent.");
		}
		var type = typeof(TSignal);
		if(SignalProcessorPriorityPerType.TryGetValue(type, out var before))
			SignalProcessorPriorityPerType[type] = [.. moduleOrder, .. before.Except(moduleOrder)];
		else
			SignalProcessorPriorityPerType[type] = [.. moduleOrder];
		return this;
	}

	/// <summary>
	/// Retrieves an <see cref="IObservable{TSignal}"/>
	/// that emits all future signals of type <typeparamref name="TSignal"/>
	/// that are sent on the agent.
	/// <br/>
	/// Use this method when configuring the agent in a method chain. Example:
	/// <code>
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
		observable = new SignalObservable<TSignal>(this);
		return this;
	}

	/// <summary>
	/// Returns an <see cref="IObservable{TSignal}"/>
	/// that emits all future signals of type <typeparamref name="TSignal"/>
	/// that are sent on the agent.
	/// <br/>
	/// Example:
	/// <code>
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
		=> new SignalObservable<TSignal>(this);

	#endregion
}
