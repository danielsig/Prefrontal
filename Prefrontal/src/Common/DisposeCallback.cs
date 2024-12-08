namespace Prefrontal.Common;
public sealed record DisposeCallback(Action OnDispose) : IDisposable
{
	public void Dispose() => OnDispose();
}
public sealed record AsyncDisposeCallback(Func<Task> OnDispose) : IAsyncDisposable
{
	public ValueTask DisposeAsync() => new(OnDispose());
}
