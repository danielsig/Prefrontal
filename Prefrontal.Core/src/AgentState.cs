namespace Prefrontal;

/// <inheritdoc cref="Agent.State"/>
/// <seealso cref="Agent.State"/>
public enum AgentState
{
	Uninitialized,
	Initializing,
	Initialized,
	Disposing,
	Disposed,
}

/// <summary>
/// Internal class that represents an agent's state as an observable stream.
/// </summary>
internal class AgentStateObservable(ILogger Debug) : IObservable<AgentState>, IDisposable
{
	private readonly Lock _gate = new();
	private IObserver<AgentState>[] _observers = [];
	internal AgentState State
	{
		get => field;
		set
		{
			IObserver<AgentState>[] observers;
			lock(_gate)
			{
				field = value;
				observers = _observers;
			}
			foreach(var observer in observers)
				try
				{
					observer.OnNext(value);
				}
				catch(Exception ex)
				{
					Debug.LogError(ex, "An error occurred while notifying an observer of the agent state.");
				}
		}
	}

	public IDisposable Subscribe(IObserver<AgentState> observer)
	{
		ArgumentNullException.ThrowIfNull(observer);

		lock(_gate)
			_observers = [.. _observers, observer];

		try
		{
			observer.OnNext(State);
		}
		catch(Exception ex)
		{
			Debug.LogError(ex, "An error occurred while notifying a new observer of the agent state.");
		}
		return new DisposeCallback(() =>
		{
			lock(_gate)
				_observers = _observers.Except(observer).ToArray();
		});
	}

	public void Dispose()
	{
		IObserver<AgentState>[] observers;
		lock(_gate)
		{
			observers = _observers;
			_observers = [];
		}
		foreach(var observer in observers)
			try
			{
				observer.OnCompleted();
			}
			catch(Exception ex)
			{
				Debug.LogError(ex, "An error occurred while notifying an observer that the agent state stream has completed.");
			}
	}
}
