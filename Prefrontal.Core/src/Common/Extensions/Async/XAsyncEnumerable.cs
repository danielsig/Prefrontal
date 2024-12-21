using System.Diagnostics.CodeAnalysis;

namespace Prefrontal.Common.Extensions.Async;

public static class XAsyncEnumerable
{

	#region Misc
	
	public static async Task ToTask<T>(this IAsyncEnumerable<T> source)
	{
		await foreach(var _ in source) { }
	}

	public static async Task<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> source)
	{
		var list = new List<T>();
		await foreach(var item in source)
			list.Add(item);
		return list;
	}

	public static async IAsyncEnumerable<TTarget> Cast<TSource, TTarget>(this IAsyncEnumerable<TSource> source)
	{
		await foreach(var item in source)
			if(item is TTarget t)
				yield return t;
	}

	/// <inheritdoc cref="Enumerable.Where{TSource}(IEnumerable{TSource}, Func{TSource, bool})"/>
	public static async IAsyncEnumerable<T> Where<T>(this IAsyncEnumerable<T> source, Func<T, bool> predicate)
	{
		await foreach(var item in source)
			if(predicate(item))
				yield return item;
	}

	/// <inheritdoc cref="Enumerable.Select{TSource, TResult}(IEnumerable{TSource}, Func{TSource, TResult})"/>
	public static async IAsyncEnumerable<TResult> Select<TSource, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, TResult> selector)
	{
		await foreach(var item in source)
			yield return selector(item);
	}

	/// <inheritdoc cref="Enumerable.Prepend{T}(IEnumerable{T}, T)"/>
	public static async IAsyncEnumerable<T> Prepend<T>(this IAsyncEnumerable<T> source, params IEnumerable<T> itemsToPrepend)
	{
		foreach(var item in itemsToPrepend)
			yield return item;
		await foreach(var item in source)
			yield return item;
	}

	/// <inheritdoc cref="Enumerable.Append{TSource}(IEnumerable{TSource}, TSource)"/>
	public static async IAsyncEnumerable<T> Append<T>(this IAsyncEnumerable<T> source, params IEnumerable<T> itemsToAppend)
	{
		await foreach(var item in source)
			yield return item;
		foreach(var item in itemsToAppend)
			yield return item;
	}

	/// <inheritdoc cref="Enumerable.Concat{T}(IEnumerable{T}, IEnumerable{T)"/>
	public static async IAsyncEnumerable<T> Concat<T>(this IAsyncEnumerable<T> first, IAsyncEnumerable<T> second)
	{
		await foreach(var item in first)
			yield return item;
		await foreach(var item in second)
			yield return item;
	}

	/// <inheritdoc cref="Enumerable.Concat{T}(IEnumerable{T}, IEnumerable{T)"/>
	public static async IAsyncEnumerable<T> Concat<T>(this IAsyncEnumerable<T> first, IEnumerable<T> second)
	{
		await foreach(var item in first)
			yield return item;
		foreach(var item in second)
			yield return item;
	}

	/// <inheritdoc cref="Enumerable.Concat{T}(IEnumerable{T}, IEnumerable{T)"/>
	public static async IAsyncEnumerable<T> Concat<T>(this IEnumerable<T> first, IAsyncEnumerable<T> second)
	{
		foreach(var item in first)
			yield return item;
		await foreach(var item in second)
			yield return item;
	}

	#endregion

	#region First & FirstOrDefault

	/// <inheritdoc cref="Enumerable.First{TSource}(IEnumerable{TSource})"/>
	public static async Task<T> FirstAsync<T>(this IAsyncEnumerable<T> source)
	{
		ArgumentNullException.ThrowIfNull(source);
		await foreach(var item in source)
			return item;
		throw new InvalidOperationException("Sequence contains no elements.");
	}

	/// <inheritdoc cref="Enumerable.FirstOrDefault{TSource}(IEnumerable{TSource}, TSource)"/>
	[return: NotNullIfNotNull(nameof(defaultValue))]
	public static async Task<T?> FirstOrDefaultAsync<T>(this IAsyncEnumerable<T> source, T? defaultValue = default)
	{
		ArgumentNullException.ThrowIfNull(source);
		await foreach(var item in source)
			return item;
		return defaultValue;
	}

	/// <inheritdoc cref="Enumerable.First{TSource}(IEnumerable{TSource}, Func{TSource, bool})"/>
	public static async Task<T> FirstAsync<T>(this IAsyncEnumerable<T> source, Func<T, bool> predicate)
	{
		ArgumentNullException.ThrowIfNull(source);
		ArgumentNullException.ThrowIfNull(predicate);
		await foreach(var item in source)
			if(predicate(item))
				return item;
		throw new InvalidOperationException("Sequence contains no elements that satisfy the predicate.");
	}

	/// <inheritdoc cref="Enumerable.FirstOrDefault{TSource}(IEnumerable{TSource}, Func{TSource, bool}, TSource)"/>
	[return: NotNullIfNotNull(nameof(defaultValue))]
	public static async Task<T?> FirstOrDefaultAsync<T>(this IAsyncEnumerable<T> source, Func<T, bool> predicate, T? defaultValue = default)
	{
		ArgumentNullException.ThrowIfNull(source);
		ArgumentNullException.ThrowIfNull(predicate);
		await foreach(var item in source)
			if(predicate(item))
				return item;
		return defaultValue;
	}

	#endregion

	#region Last & LastOrDefault

	/// <inheritdoc cref="Enumerable.Last{TSource}(IEnumerable{TSource})"/>
	public static async Task<T> LastAsync<T>(this IAsyncEnumerable<T> source)
	{
		ArgumentNullException.ThrowIfNull(source);
		var hasItem = false;
		T item = default!;
		await foreach(var i in source)
		{
			hasItem = true;
			item = i;
		}
		if(hasItem)
			return item;
		throw new InvalidOperationException("Sequence contains no elements.");
	}

	/// <inheritdoc cref="Enumerable.LastOrDefault{TSource}(IEnumerable{TSource}, TSource)"/>
	[return: NotNullIfNotNull(nameof(defaultValue))]
	public static async Task<T?> LastOrDefaultAsync<T>(this IAsyncEnumerable<T> source, T? defaultValue = default)
	{
		ArgumentNullException.ThrowIfNull(source);
		var hasItem = false;
		T item = default!;
		await foreach(var i in source)
		{
			hasItem = true;
			item = i;
		}
		if(hasItem)
			return item;
		return defaultValue;
	}

	/// <inheritdoc cref="Enumerable.Last{TSource}(IEnumerable{TSource}, Func{TSource, bool})"/>
	public static async Task<T> LastAsync<T>(this IAsyncEnumerable<T> source, Func<T, bool> predicate)
	{
		ArgumentNullException.ThrowIfNull(source);
		ArgumentNullException.ThrowIfNull(predicate);
		var hasItem = false;
		T item = default!;
		await foreach(var i in source)
			if(predicate(i))
			{
				hasItem = true;
				item = i;
			}
		if(hasItem)
			return item;
		throw new InvalidOperationException("Sequence contains no elements that satisfy the predicate.");
	}

	/// <inheritdoc cref="Enumerable.LastOrDefault{TSource}(IEnumerable{TSource}, Func{TSource, bool}, TSource)"/>
	[return: NotNullIfNotNull(nameof(defaultValue))]
	public static async Task<T?> LastOrDefaultAsync<T>(this IAsyncEnumerable<T> source, Func<T, bool> predicate, T? defaultValue = default)
	{
		ArgumentNullException.ThrowIfNull(source);
		ArgumentNullException.ThrowIfNull(predicate);
		var hasItem = false;
		T item = default!;
		await foreach(var i in source)
			if(predicate(i))
			{
				hasItem = true;
				item = i;
			}
		if(hasItem)
			return item;
		return defaultValue;
	}

	#endregion
}
