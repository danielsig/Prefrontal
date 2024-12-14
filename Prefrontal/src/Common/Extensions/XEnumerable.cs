using System.Collections;
using System.Text;

namespace Prefrontal.Common.Extensions;

/// <summary> Provides extension methods for <see cref="IEnumerable{T}"/>. </summary>
public static class XEnumerable
{
	/// <summary>
	/// Just like <see cref="string.Join(string, IEnumerable{string})"/>, but with a default separator ", ".
	/// </summary>
	/// <inheritdoc cref="string.Join(string, IEnumerable{string})" />
	public static string Join<T>(
		this IEnumerable<T> values,
		string separator = ", "
	) => string.Join(separator ?? ", ", values);

	/// <summary>
	/// Just like <see cref="Join{T}(IEnumerable{T}, string)"/>, but with a last separator.
	/// </summary>
	/// <inheritdoc cref="string.Join(string, IEnumerable{string})" path="/param"/>
	/// <param name="lastSeparator">The separator to use before the last item.</param>
	/// <returns>
	/// 	A string that consists of the elements of <paramref name="values"/>
	/// 	delimited by the <paramref name="separator"/> string and finally the <paramref name="lastSeparator"/> string
	/// 	-or- string.Empty if <paramref name="values"/> has zero elements.
	/// </returns>
	public static string JoinVerbose<T>(
		this IEnumerable<T> values,
		string separator = ", ",
		string lastSeparator = " and "
	)
	{
		ArgumentNullException.ThrowIfNull(values, nameof(values));
		separator ??= ", ";
		lastSeparator ??= separator;

		using var n = values.GetEnumerator();
		if(n.MoveNext())
		{
			var sb = new StringBuilder();
			sb.Append(n.Current);
			if(n.MoveNext())
			{
				T prev = n.Current;
				while(n.MoveNext())
				{
					sb.Append(separator)
						.Append(prev);
					prev = n.Current;
				}
				sb.Append(lastSeparator)
					.Append(prev);
			}
			return sb.ToString();
		}
		return string.Empty;
	}

	/// <summary>
	/// Maps the <typeparamref name="TEnumerable"/>
	/// to a single <typeparamref name="TOut"/> value.
	/// Handy when you need a run a block of code in an expression.
	/// <br/>
	/// Example:
	/// <code>
	/// var avgAndStd = new List&lt;double&gt; { 1, 2, 3 }
	/// 	.Select(x => x * 2)
	/// 	.Thru(list =>
	/// 	{
	/// 		double average = list.Average();
	/// 		double standardDeviation = Math.Sqrt(list.Average(x => Math.Pow(x - average, 2)));
	/// 		return (average, standardDeviation);
	/// 	});
	/// </code>
	/// </summary>
	public static TOut Thru<TEnumerable, TOut>(
		this TEnumerable enumerable,
		Func<TEnumerable, TOut> func
	)
	where TEnumerable : IEnumerable
	{
		return func(enumerable);
	}


	/// <summary>
	/// Executes an action on the <typeparamref name="TEnumerable"/>
	/// and returns the original <typeparamref name="TEnumerable"/>.
	/// Handy when you need to run a block of code in an expression for side effects.
	/// <br/>
	/// Example:
	/// <code>
	/// var sum = new List&lt;double&gt; { 1, 2, 3 }
	/// 	.Select(x => x * 2)
	/// 	.Tap(list => Console.WriteLine("Doubled: {list.JoinVerbose()}"))
	/// 	.Where(x => x > 3)
	/// 	.Tap(list => Console.WriteLine("Filtered: {list.JoinVerbose()}"))
	/// 	.Sum();
	/// </code>
	/// </summary>
	public static TEnumerable Tap<TEnumerable>(
		this TEnumerable enumerable,
		Action<TEnumerable> action
	)
	where TEnumerable : IEnumerable
	{
		action(enumerable);
		return enumerable;
	}

	/// <summary>
	/// Executes a function on the <typeparamref name="TEnumerable"/>,
	/// assigns the result to <paramref name="output"/>,
	/// and returns the original <typeparamref name="TEnumerable"/>.
	/// Handy when you need to run a block of code in an expression for side effects.
	/// <br/>
	/// Example:
	/// <code>
	/// var allExceptMax = new List&lt;double&gt; { 1, 2, 3 }
	/// 	.Select(x => x * 2)
	/// 	.Tap(Enumerable.Max, out var max)
	/// 	.Except(max)
	/// 	.ToList();
	/// </code>
	/// </summary>
	public static TEnumerable Tap<TEnumerable, T>(
		this TEnumerable enumerable,
		Func<TEnumerable, T> func,
		out T output
	)
	where TEnumerable : IEnumerable<T>
	{
		output = func(enumerable);
		return enumerable;
	}

	/// <inheritdoc cref="Enumerable.Except{TSource}(IEnumerable{TSource}, IEnumerable{TSource})"/>
	public static IEnumerable<T> Except<T>(
		this IEnumerable<T> enumerable,
		params T[] exceptions
	) => Enumerable.Except(enumerable, exceptions);
}
