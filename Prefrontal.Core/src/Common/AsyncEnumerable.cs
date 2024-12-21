namespace Prefrontal.Common;

public static class AsyncEnumerable
{
	/// <summary>
	/// Creates an <see cref="IAsyncEnumerable{T}"/> from a single value.
	/// </summary>
	/// <typeparam name="T">The type of the value.</typeparam>
	/// <param name="value">The value to create an <see cref="IAsyncEnumerable{T}"/> from.</param>
	/// <returns>An <see cref="IAsyncEnumerable{T}"/> that yields the single value.</returns>
	public static async IAsyncEnumerable<T> FromValue<T>(T value)
	{
		yield return value;
		await Task.CompletedTask;
	}

	/// <summary>
	/// Creates an empty <see cref="IAsyncEnumerable{T}"/>
	/// </summary>
	/// <typeparam name="T">The type of the value.</typeparam>
	/// <returns>An empty <see cref="IAsyncEnumerable{T}"/>.</returns>
	/// <remarks>
	/// This method is equivalent to <see cref="Enumerable.Empty{T}"/>.
	/// </remarks>
	public static async IAsyncEnumerable<T> Empty<T>()
	{
		await Task.CompletedTask;
		yield break;
	}
}
