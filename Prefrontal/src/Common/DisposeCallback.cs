namespace Prefrontal.Common;

/// <summary>
/// Represents a disposable object that invokes a callback when disposed.
/// </summary>
/// <param name="OnDispose">The callback to invoke when the object is disposed.</param>
public sealed record DisposeCallback(Action OnDispose) : IDisposable
{
	public void Dispose() => OnDispose();
}
