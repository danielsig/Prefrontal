namespace Prefrontal.Common.Extensions.Async;

public static class XTask
{
	/// <inheritdoc cref="Then{TIn, TOut}"/>
	public static async Task Then(this Task task, Action then)
	{
		await task;
		then();
	}
	/// <inheritdoc cref="Then{TIn, TOut}"/>
	public static async Task<TOut> Then<TOut>(this Task task, Func<TOut> then)
	{
		await task;
		return then();
	}
	/// <summary>
	/// Just like then in JavaScript.
	/// </summary>
	public static async Task<TOut> Then<TIn, TOut>(this Task<TIn> task, Func<TIn, TOut> then)
		=> then(await task);

	/// <inheritdoc cref="Then{TIn, TOut}"/>
	public static async Task<TOut> Then<TIn, TOut>(this Task<TIn> task, Func<TIn, Task<TOut>> then)
		=> await then(await task);
	
	public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this Task<T> task)
	{
		yield return await task;
	}
}
