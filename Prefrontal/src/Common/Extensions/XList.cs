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
		var foo = new List<int>{1, 2, 3};
		var temp = foo.RemoveLast();

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

	/// <summary>
	/// Removes the first element from the list and returns it
	/// or <see langword="null"/> if the list is empty.
	/// </summary>
	/// <typeparam name="T">The type of the elements in the list</typeparam>
	/// <param name="list">The list whose first element to remove</param>
	/// <returns>The first element of the list, or <see langword="null"/> if the list is empty</returns>
	public static T? RemoveFirst<T>(this IList<T> list)
	where T : class
	{
		if(list.Count == 0)
			return null;

		T item = list[0];
		list.RemoveAt(0);
		return item;
	}


	/// <inheritdoc cref="RemoveFirst{T}(IList{T})"/>
	public static T? RemoveFirst<T>(this List<T> list)
	where T : struct
	{
		if(list.Count == 0)
			return null;
		
		T? item = list[0];
		list.RemoveAt(0);
		return item;
	}

	/// <summary>
	/// Removes the last element from the list and returns it
	/// or <see langword="null"/> if the list is empty.
	/// </summary>
	/// <typeparam name="T">The type of the elements in the list</typeparam>
	/// <param name="list">The list whose last element to remove</param>
	/// <returns>The last element of the list, or <see langword="null"/> if the list is empty</returns>
	public static T? RemoveLast<T>(this IList<T> list)
	where T : class
	{
		if(list.Count == 0)
			return null;

		T item = list[^1];
		list.RemoveAt(list.Count - 1);
		return item;
	}

	/// <inheritdoc cref="RemoveLast{T}(IList{T})"/>
	public static T? RemoveLast<T>(this List<T> list)
	where T : struct
	{
		if(list.Count == 0)
			return null;

		T? item = list[^1];
		list.RemoveAt(list.Count - 1);
		return item;
	}
}
