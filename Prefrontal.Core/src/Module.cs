namespace Prefrontal;

/// <summary>
/// A base class for all modules that can be added to an <see cref="Prefrontal.Agent"/>.
/// <para>
/// 	Refer to <a href="Prefrontal.Agent.yml#agent_lifecycle">Agent's Lifecycle</a>
/// 	for information on what can and cannot be done in each of the agent's lifecycle stages.
/// </para>
/// <para>
/// 	Each module's constructor can have injectable dependencies in its parameters
/// 	which are injected by the agent using its <see cref="Agent.ServiceProvider"/>.
/// 	The module's constructor can also take the agent itself
/// 	as a parameter and even other modules that it requires.
/// </para>
/// <list type="bullet">
/// 	<item>
///			Modules can be added to an agent
///			by calling <see cref="Agent.AddModule{T}">AddModule&lt;T&gt;()</see>.
///		</item>
/// 	<item>
///			Modules can be removed from an agent
///			by calling <see cref="Agent.RemoveModule{T}">RemoveModule&lt;T&gt;()</see>.
///		</item>
///		<item>
///			Call <see cref="Initialize">Initialize()</see>
///			or <see cref="InitializeAsync">InitializeAsync()</see>
///			to initialize the agent and all of its modules.
///		</item>
///		<item>
///			Modules can send signals to other modules on the same agent
///			by calling <see cref="SendSignal{TSignal}(TSignal)"/>
///			or <see cref="SendSignalAsync{TSignal}(TSignal)"/>.
///			These signals can be of any type,
///			but it's best to create your own signal types.
///		</item>
///		<item>
///			Signals can also return a response that the sender can receive
///			by calling <see cref="SendSignal{TSignal, TResponse}(TSignal, bool)"/>
///			or <see cref="SendSignalAsync{TSignal, TResponse}(TSignal)"/>.
///			Signal processors and senders are not matched by the response type,
///			only by the signal type. This means that if the response type
///			 of a signal processor does not match the expected response type
///			 of the sender, the response will be empty.
///		</item>
///		<item>
///			Modules can implement <see cref="IDisposable"/>
/// 		if they need to clean up resources when they are removed from the agent.
/// 		Modules can also block the agent from removing them
/// 		by throwing an <see cref="InvalidOperationException"/>
/// 		in their <see cref="IDisposable.Dispose"/> method.
/// 		However, doing so when the agent itself is being disposed of has no effect.
/// 	</item>
/// 	<item>
/// 		Modules can log messages using the <see cref="Debug"/> property.
/// 	</item>
/// </list>
/// </summary>
public abstract class Module
{
	/// <summary>
	/// The agent that this module belongs to.
	/// <em><b>Beware that this can be null if the module has been removed from the agent.</b></em>
	/// To check if the module is still part of the agent, you can <see cref="implicit operator bool">implicitly cast the module to a boolean</see>.
	/// </summary>
	public Agent Agent
	{
		get => _agent
			?? throw new InvalidOperationException(
				_removed
					? "The module has been removed from the agent."
					: "The module has not yet been added to the agent."
					+ " Please add an Agent parameter to the module's constructor"
					+ " and pass it to the base constructor"
					+ " to gain access to the agent inside the module's constructor."
			);
		internal set
		{
			_agent = value;
			if(value is not null)
			{
				if(_onAddedToAgent.Count == 0)
					return;
				foreach(var action in _onAddedToAgent)
					action();
				_onAddedToAgent.Clear();
			}
			else
			{
				_removed = true;
				if(_signalers.Count == 0)
					return;
				foreach(var signaler in _signalers)
					signaler.RemoveModule(this);
				_signalers.Clear();
			}
		}
	}

	/// <summary>
	/// Default base constructor.
	/// <br/>
	/// There is no need to call this constructor in derived classes.
	/// <br/>
	/// Do not instantiate modules directly except in the createModule
	/// callback passed to <see cref="Agent.AddModule{T}(Func{Agent, T})"/>.
	/// </summary>
	protected Module() { }

	/// <summary>
	/// Base constructor that takes the agent as a parameter.
	/// Call this base constructor in derived classes when you need access to <see cref="Agent"/>
	/// inside the module's constructor like so:
	/// <code language="csharp">
	/// public class MyModule : Module
	/// {
	/// 	public MyModule(Agent agent) : base(agent)
	/// 		=> Debug.Log("MyModule has been added to the agent: {agent}", agent.Name);
	/// }
	/// </code>
	/// <br/>
	/// Do not instantiate modules directly except in the createModule
	/// callback passed to <see cref="Agent.AddModule{T}(Func{Agent, T})"/>.
	/// </summary>
	protected Module(Agent agent) => Agent = agent;

	/// <summary>
	/// The logger that this module can use to log messages.
	/// </summary>
	protected internal ILogger Debug => Agent.Debug;

	/// <summary>
	/// Module specific initialization logic goes in here.
	/// <list type="bullet">
	/// 	<item>
	/// 		<em><b>Do not call this method directly.</b></em>
	/// 	</item>
	/// 	<item>
	/// 		Calling <see cref="Agent.Initialize">Initialize</see>
	/// 		or <see cref="Agent.InitializeAsync">InitializeAsync</see>
	/// 		on the agent will initialize all of its modules
	/// 		in the same order as they were added to the agent.
	/// 	</item>
	/// 	<item>
	/// 		Module initialization is run sequentially, not in parallel.
	/// 		In other words, each module's initialization will not start
	/// 		until the previous module's initialization has completed.
	/// 	</item>
	/// 	<item>
	///			There is no need to override both <see cref="Module.Initialize"/>
	///			and <see cref="Module.InitializeAsync"/> because
	///			in that case only the async method gets called.
	/// 	</item>
	/// </list>
	/// </summary>
	protected internal virtual void Initialize()
	{
		
	}

	/// <inheritdoc cref="Initialize()"/>
	/// <remarks>
	/// 	This method takes precedence over <see cref="Initialize"/>
	/// 	if both are overridden in the same module.
	/// </remarks>
	/// <returns>A task that represents the asynchronous initialization of this module.</returns>
	protected internal virtual Task InitializeAsync()
	{
		Initialize();
		return Task.CompletedTask;
	}

	/// <inheritdoc cref="Agent.SendSignal{TSignal, TResponse}(TSignal, bool)"/>
	protected IEnumerable<TResponse> SendSignal<TSignal, TResponse>(TSignal signal, bool synchronous)
		=> Agent.SendSignal<TSignal, TResponse>(signal, synchronous);

	/// <inheritdoc cref="Agent.SendSignalAsync{TSignal, TResponse}(TSignal)"/>
	protected IAsyncEnumerable<TResponse> SendSignalAsync<TSignal, TResponse>(TSignal signal)
		=> Agent.SendSignalAsync<TSignal, TResponse>(signal);

	/// <inheritdoc cref="Agent.SendSignal{TSignal}(TSignal)"/>
	protected void SendSignal<TSignal>(TSignal signal)
		=> Agent.SendSignal(signal);
	
	/// <inheritdoc cref="Agent.SendSignalAsync{TSignal}(TSignal)"/>
	protected Task SendSignalAsync<TSignal>(TSignal signal)
		=> Agent.SendSignalAsync(signal);

	/// <summary>
	/// Registers a receiver for signals of type <typeparamref name="TSignal"/>
	/// that returns a response of type <typeparamref name="TResponse"/>.
	/// <para>
	/// 	The receiver and sender are only matched
	/// 	by the <typeparamref name="TSignal"/> type
	/// 	and not by the <typeparamref name="TResponse"/> type.
	/// 	If the return value of a receiver cannot be cast to the
	/// 	response type that the sender expects, the response will be empty.
	/// </para>
	/// <para>
	/// 	There is no need to dispose of the returned <see cref="IDisposable"/>
	/// 	unless you want to stop receiving signals. This is because the agent
	/// 	will automatically unsubscribe the receiver when the module is removed.
	/// </para>
	/// </summary>
	protected IDisposable ReceiveSignals<TSignal, TResponse>(Func<TSignal, TResponse> receiver)
		=> SubscribeToSignals<TSignal>(subject =>
			new Signaler<TSignal>.ReceiverProcessor<TResponse>(subject, receiver, this)
		);
	
	/// <inheritdoc cref="ReceiveSignals{TSignal, TResponse}(Func{TSignal, TResponse})"/>
	protected IDisposable ReceiveSignalsAsync<TSignal, TResponse>(Func<TSignal, Task<TResponse>> receiver)
		=> SubscribeToSignals<TSignal>(subject =>
			new Signaler<TSignal>.AsyncReceiverProcessor<TResponse>(subject, receiver, this)
		);

	/// <summary>
	/// Registers a receiver for signals of type <typeparamref name="TSignal"/>.
	/// <para>
	/// 	The receiver will be called even if the sender expects a response.
	/// 	However, the response will be empty
	/// 	if there is no other receiver that returns a response.
	/// </para>
	/// <para>
	/// 	There is no need to dispose of the returned <see cref="IDisposable"/>
	/// 	unless you want to stop receiving signals. This is because the agent
	/// 	will automatically unsubscribe the receiver when the module is removed.
	/// </para>
	/// </summary>
	protected IDisposable ReceiveSignals<TSignal>(Action<TSignal> receiver)
		=> SubscribeToSignals<TSignal>(subject =>
			new Signaler<TSignal>.ReceiverProcessor(subject, receiver, this)
		);

	/// <inheritdoc cref="ReceiveSignals{TSignal}(Action{TSignal})"/>
	protected IDisposable ReceiveSignalsAsync<TSignal>(Func<TSignal, Task> receiver)
		=> SubscribeToSignals<TSignal>(subject =>
			new Signaler<TSignal>.AsyncReceiverProcessor(subject, receiver, this)
		);


	/// <summary>
	/// Registers a signal interceptor for signals of type <typeparamref name="TSignal"/>
	/// with a response of type <typeparamref name="TResponse"/>.
	/// <para>
	/// 	Interceptors differ from receivers in that they can:
	/// 	<list type="number">
	/// 		<item>
	/// 			Change the signal before passing it to subsequent processors
	/// 			by calling <see cref="SignalContext{TSignal, TResponse}.Next(TSignal)">context.Next(new signal)</see>.
	/// 		</item>
	/// 		<item>
	/// 			Stop subsequent signal processing by returning
	/// 			without calling <see cref="SignalContext{TSignal, TResponse}.Next">context.Next()</see>.
	/// 		</item>
	/// 		<item>
	/// 			Modify the responses returned from
	/// 			<see cref="SignalContext{TSignal, TResponse}.Next">subsequent signal processors</see>
	/// 			before returning it to the sender.
	/// 		</item>
	/// 	</list>
	/// </para>
	/// <para>
	/// 	The interceptor and sender are only matched
	/// 	by the <typeparamref name="TSignal"/> type
	/// 	and not by the <typeparamref name="TResponse"/> type.
	/// 	If the return value of an interceptor cannot be cast to the
	/// 	response type that the sender expects, the response will be empty.
	/// </para>
	/// <para>
	/// 	There is no need to dispose of the returned <see cref="IDisposable"/>
	/// 	unless you want to stop intercepting signals. This is because the agent
	/// 	will automatically unsubscribe the interceptor when the module is removed.
	/// </para>
	/// </summary>
	/// <param name="interceptor">
	/// 	A function that takes a <see cref="SignalContext{TSignal, TResponse}"/>
	/// 	and returns an <see cref="IAsyncEnumerable{TResponse}"/>.
	/// 	Calling <c>context.Next()</c> will continue the signal processing.
	/// </param>
	/// <seealso cref="SignalContext{TSignal, TResponse}"/>
	protected IDisposable InterceptSignalsAsync<TSignal, TResponse>(Func<SignalContext<TSignal, TResponse>, IAsyncEnumerable<TResponse>> interceptor)
		=> SubscribeToSignals<TSignal>(subject =>
			new Signaler<TSignal>.AsyncInterceptorProcessor<TResponse>(subject, interceptor, this)
		);

	/// <inheritdoc cref="InterceptSignalsAsync{TSignal, TResponse}(Func{SignalContext{TSignal, TResponse}, IAsyncEnumerable{TResponse}})"/>
	protected IDisposable InterceptSignalsAsync<TSignal>(Func<SignalContext<TSignal, object>, IAsyncEnumerable<object>> interceptor)
		=> SubscribeToSignals<TSignal>(subject =>
			new Signaler<TSignal>.AsyncInterceptorProcessor<object>(subject, interceptor, this)
		);

	/// <summary>
	/// 	Unless overridden,
	/// 	returns the name of the module's type
	/// 	(without the "Module" suffix).
	/// 	<br/>
	/// 	Generic type arguments are also included in the output, if any.
	/// </summary>
	public override string ToString() => TypeName;

	/// <summary>
	/// True if the module is still part of the agent, false otherwise.
	/// </summary>
	public static implicit operator bool(Module module)
		=> module is { _agent: not null };


	#region private & internal

	private Agent? _agent;
	private bool _removed = false;
	private string? _typeName;
	internal string TypeName
	{
		get
		{
			if(_typeName is not null)
				return _typeName;

			var type = GetType();
			var name = type.Name;
			if(name.EndsWith("Module"))
				name = name[..^6];
			if(type.IsGenericType)
				name += '<'
					+ type.GetGenericArguments()
						.Select(XType.ToVerboseString)
						.Join()
					+ '>';

			return _typeName = name;
		}
	}

	/// <summary>
	/// Used to defer certain operations that require access
	/// to <see cref="Agent"/> until it has been assigned.
	/// <br/>
	/// These operations are typically signal subscriptions
	/// created in the module's constructor.
	/// </summary>
	private readonly List<Action> _onAddedToAgent = [];
	/// <summary>
	/// Used to remove references to this module relevant signalers
	/// when this module is removed from the agent.
	/// <br/>
	/// This is redundant when the agent is being disposed of
	/// since the signalers themselves will also be disposed of.
	/// </summary>
	internal readonly HashSet<Signaler> _signalers = [];
	private IDisposable SubscribeToSignals<TSignal>(
		Func<Signaler<TSignal>, Signaler<TSignal>.Processor> createProcessor
	)
	{
		if(_agent is null)
		{
			if(_removed)
				throw new InvalidOperationException("The module has been removed from the agent.");
			
			IDisposable? disposable = null;
			_onAddedToAgent.Add(() => disposable = SubscribeToSignals(createProcessor));
			return new DisposeCallback(() => disposable?.Dispose());
		}

		var signaler = Agent.GetSignaler<TSignal>();
		_signalers.Add(signaler);
		return signaler.Subscribe(createProcessor(signaler));
	}
	#endregion
}
