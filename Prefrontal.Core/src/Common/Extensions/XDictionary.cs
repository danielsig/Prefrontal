namespace Prefrontal.Common.Extensions;


/// <summary> Provides extension methods for <see cref="IDictionary{TKey, TValue}"/>. </summary>
public static class XDictionary
{
	/// <summary>
	/// Gets the value associated with the specified key or adds a new value if the key is not found.
	/// </summary>
	/// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
	/// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
	/// <param name="valueFactory">The function used to generate a new value in case the key is not found.</param>
	/// <returns>The value associated with the specified key or the new value.</returns>
	public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TValue> valueFactory)
	{
		if(dictionary.TryGetValue(key, out TValue? value))
			return value;
		value = valueFactory();
		dictionary.Add(key, value);
		return value;
	}

	/// <inheritdoc cref="GetOrAdd{TKey, TValue}(IDictionary{TKey, TValue}, TKey, Func{TValue})"/>
	public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key) where TValue : new()
	{
		if(dictionary.TryGetValue(key, out TValue? value))
			return value;
		value = new TValue();
		dictionary.Add(key, value);
		return value;
	}
}
