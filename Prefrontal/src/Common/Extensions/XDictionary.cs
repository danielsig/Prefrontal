namespace Prefrontal.Common.Extensions;
public static class XDictionary
{
	public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TValue> valueFactory)
	{
		if(dictionary.TryGetValue(key, out TValue? value))
			return value;
		value = valueFactory();
		dictionary.Add(key, value);
		return value;
	}
	public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key) where TValue : new()
	{
		if(dictionary.TryGetValue(key, out TValue? value))
			return value;
		value = new TValue();
		dictionary.Add(key, value);
		return value;
	}
}
