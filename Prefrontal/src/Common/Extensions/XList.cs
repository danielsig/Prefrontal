namespace Prefrontal.Common.Extensions;

public static class XList
{
	/// <summary>
	/// Extracts all elements from a list that satisfy a predicate.
	/// This method mutates the list.
	/// </summary>
	/// <param name="list">The list to extract elements from.</param>
	/// <param name="predicate">The predicate to test elements against.</param>
	/// <returns>The elements that were removed</returns>
	public static List<T> Extract<T>(this List<T> list, Func<T, bool> predicate)
	{
		int i = 0;
		List<T> extraction = [];
		try
		{
			for(i = 0; i < list.Count; ++i)
			{
				if(predicate(list[i]))
					extraction.Add(list[i]);
				else
					list[i - extraction.Count] = list[i];
			}
		}
		finally
		{
			if(extraction.Count > 0)
				list.RemoveRange(list.Count - extraction.Count, extraction.Count);
		}
		return extraction;
	}
}
