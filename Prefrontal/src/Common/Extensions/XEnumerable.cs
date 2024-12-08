using System.Text;

namespace Prefrontal.Common.Extensions;

public static class XEnumerable
{
	/// <summary>
	/// Just like <see cref="string.Join(string, IEnumerable{string})"/>, but with a last separator.
	/// </summary>
	public static string JoinVerbose<T>(
		this IEnumerable<T> arr,
		string separator = ", ",
		string lastSeparator = " and "
	)
	{
		var sb = new StringBuilder();
		using var n = arr.GetEnumerator();
		if(n.MoveNext())
		{
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
		}
		return sb.ToString();
	}
}
