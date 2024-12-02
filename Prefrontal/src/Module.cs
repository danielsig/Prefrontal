namespace Prefrontal;

/// <summary>
/// A base class for all modules that can be added to an <see cref="Prefrontal.Agent"/>.
/// </summary>
public abstract class Module : IDisposable
{
	/// <summary>
	/// The agent that this module belongs to.
	/// </summary>
	public Agent Agent { get; internal set; } = null!;

	public virtual void Initialize() // Should this be async? 🤔
	{

	}

	public virtual void Dispose()
	{
		GC.SuppressFinalize(this);
	}
}
