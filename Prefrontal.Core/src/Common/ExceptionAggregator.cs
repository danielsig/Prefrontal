namespace Prefrontal.Common;

public sealed class ExceptionAggregator : IDisposable
{
	private List<Exception>? _exceptions = null;
	public void Add(Exception exception)
	{
		if(exception is not null)
			(_exceptions ??= []).Add(exception);
	}
	public void Try(Action action)
	{
		try
		{
			action();
		}
		catch(Exception exception)
		{
			(_exceptions ??= []).Add(exception);
		}
	}
	public void Dispose()
	{
		if(_exceptions is not null)
			throw _exceptions.Count == 1
				? _exceptions[0]
				: new AggregateException(_exceptions);
	}
	public bool HasErrors => _exceptions is not null;
}

public sealed class ExceptionAggregator<T>(Func<List<(Exception exception, T value)>, string>? GetMessage = null) : IDisposable
{
	private List<(Exception exception, T value)>? _exceptions = null;
	public void Add(Exception exception, T value)
	{
		if(exception is not null)
			(_exceptions ??= []).Add((exception, value));
	}
	public void Dispose()
	{
		if(_exceptions is not null)
			throw _exceptions.Count == 1 && GetMessage is null
				? _exceptions[0].exception
				: new AggregateException(
					GetMessage?.Invoke(_exceptions),
					_exceptions.Select(e => e.exception)
				);
	}
	public bool HasErrors => _exceptions is not null;
}

