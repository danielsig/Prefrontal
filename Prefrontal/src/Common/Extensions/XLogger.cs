namespace Prefrontal.Common.Extensions;

internal static class XLogger
{
	public static IDisposable? BeginScopeValues(this ILogger logger, Dictionary<string, object> scope)
	{
		return logger.BeginScope(scope);
	}
	public static IDisposable? BeginScopeValues(this ILogger logger, params IEnumerable<(string Key, object Value)> scope)
	{
		return logger.BeginScope(scope.ToDictionary(x => x.Key, x => x.Value));
	}
}
