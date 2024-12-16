namespace Prefrontal.Common;

/// <summary>
/// Represents an asynchronous disposable object that invokes a callback when disposed.
/// </summary>
/// <param name="OnDisposeAsync">The callback to invoke when the object is disposed.</param>
public sealed record AsyncDisposeCallback(Func<ValueTask> OnDisposeAsync) : IAsyncDisposable
{
	public ValueTask DisposeAsync() => OnDisposeAsync();
}
