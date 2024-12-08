namespace Prefrontal.Common.Extensions;

public static class XString
{
	public static string? NullIfEmpty(this string? value) => string.IsNullOrEmpty(value) ? null : value;
	public static string? NullIfWhiteSpace(this string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
