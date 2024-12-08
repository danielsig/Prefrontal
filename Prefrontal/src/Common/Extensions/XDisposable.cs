namespace Prefrontal.Common.Extensions;

public static class XDisposable
{
	/// <summary>
	/// Use the disposable object for a single operation.
	/// This method ensures that the disposable object is disposed of after the operation is completed.
	/// </summary>
	/// <typeparam name="T">Type of the disposable object.</typeparam>
	/// <typeparam name="TOut">Type of the operation's result.</typeparam>
	/// <param name="disposable">An object that implements IDisposable</param>
	/// <param name="asyncDisposable">An object that implements IAsyncDisposable</param>
	/// <param name="operation">The operation to perform while using the disposable object.</param>
	public static void UseFor<T>(this T disposable, Action<T> operation) where T : IDisposable
	{
		try
		{
			operation(disposable);
		}
		finally
		{
			disposable.Dispose();
		}
	}

	/// <inheritdoc cref="UseFor{T}(T, Action{T})"/>
	public static TOut UseFor<T, TOut>(this T disposable, Func<T, TOut> operation) where T : IDisposable
	{
		try
		{
			return operation(disposable);
		}
		finally
		{
			disposable.Dispose();
		}
	}

	/// <inheritdoc cref="UseFor{T}(T, Action{T})"/>
	public static async Task UseForAsync<T>(this T asyncDisposable, Func<T, Task> operation) where T : IAsyncDisposable
	{
		try
		{
			await operation(asyncDisposable);
		}
		finally
		{
			await asyncDisposable.DisposeAsync();
		}
	}

	/// <inheritdoc cref="UseFor{T}(T, Action{T})"/>
	public static async Task<TOut> UseForAsync<T, TOut>(this T asyncDisposable, Func<T, Task<TOut>> operation) where T : IAsyncDisposable
	{
		try
		{
			return await operation(asyncDisposable);
		}
		finally
		{
			await asyncDisposable.DisposeAsync();
		}
	}
}
