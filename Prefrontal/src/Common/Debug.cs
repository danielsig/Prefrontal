namespace Prefrontal.Common;

// TODO: Make this configurable outside the prefrontal library.
public static class Debug
{
	/// <summary>
	/// Logs a message to the console.
	/// </summary>
	/// <param name="log">The message to log</param>
	public static void Log(object message)
	{
		LogStartOfLog();
		Console.WriteLine(message);
	}

	/// <summary>
	/// Logs a warning to the console.
	/// </summary>
	/// <param name="warning">The warning to log</param>
	public static void LogWarning(object warning)
	{
		var color = Console.ForegroundColor;
		Console.ForegroundColor = ConsoleColor.Yellow;
		LogStartOfLog();
		Console.WriteLine(warning);
		Console.ForegroundColor = color;
	}

	/// <summary>
	/// Logs an exception to the console.
	/// </summary>
	/// <param name="error">The exception to log</param>
	public static void LogException(Exception error)
	{
		var color = Console.ForegroundColor;
		Console.ForegroundColor = ConsoleColor.Red;
		LogStartOfLog();
		Console.WriteLine(error);
		Console.ForegroundColor = color;
	}

	private static void LogStartOfLog()
	{
		Console.Write("[" + DateTime.UtcNow.ToString("YYYY.MM.DD HH:mm:ss") + "] ");
	}
}
