namespace Prefrontal;

/// <summary>
/// Defines what happens when an exception occurs
/// in a module's <see cref="Module.RunAsync">RunAsync()</see> method.
/// </summary>
public enum RunningModuleExceptionPolicy
{
	/// <summary>
	/// <em>Default behavior.</em><br/>
	/// Logs the exception and stops running the module that threw the exception.
	/// <br/> All other modules will continue running.
	/// </summary>
	LogAndStopModule,
	/// <summary>
	/// Logs the exception and removes the module that threw the exception.
	/// <br/> All other modules will continue running.
	/// </summary>
	LogAndRemoveModule,
	/// <summary>
	/// Logs the exception and reruns the module that threw the exception.
	/// <br/> All other modules will continue running.
	/// </summary>
	LogAndRerunModule,
	/// <summary>
	/// Logs the exception, stops all modules and reruns them all.
	/// </summary>
	LogAndRerunAll,
	/// <summary>
	/// Logs the exception and stops running the other modules.
	/// <br/> This means the agent will stop running after the first exception.
	/// </summary>
	LogAndStopAll,
	/// <summary>
	/// Rethrows the exception and stops running the other modules.
	/// <br/> This means the agent will stop running after the first exception.
	/// </summary>
	RethrowAndStopAll,
}
