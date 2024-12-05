namespace Prefrontal;

/// <summary>
/// A base class for all modules that can be added to an <see cref="Prefrontal.Agent"/>.
/// <para>
/// 	Each module's constructor can have injectable dependencies in its parameters
/// 	which are injected by the agent using its <see cref="Agent.ServiceProvider"/>.
/// </para>
/// <list>
/// 	<item>Modules can be added to an agent using the <see cref="Agent.AddModule{T}"/> method.</item>
/// 	<item>Modules can be removed from an agent using the <see cref="Agent.RemoveModule{T}"/> method.</item>
///		<item>Call <see cref="Agent.Initialize"/> after adding all modules to the agent to initialize all of its modules.</item>
/// </list>
/// </summary>
public abstract class Module
{
	/// <summary>
	/// The agent that this module belongs to.
	/// </summary>
	public Agent Agent { get; internal set; } = null!;

	/// <summary>
	/// Initializes the module.
	/// This is where you should set up any connections to other modules.
	/// </summary>
	public virtual void Initialize()
	{

	}
}
