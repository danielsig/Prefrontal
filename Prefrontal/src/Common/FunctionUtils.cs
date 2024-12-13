namespace Prefrontal.Common;

public static class FunctionUtils
{
	/// <summary>
	/// A helper function to enable the use of code blocks,
	/// i.e. multiple statements, within expressions.
	/// For example, when using switch expressions
	/// where just one of the cases requires multiple statements,
	/// this function lets you use a code block
	/// instead of having to create a separate method
	/// or convert the switch expression to a switch statement.
	/// </summary>
	/// <typeparam name="TIn">The type of the input value</typeparam>
	/// <typeparam name="TOut">The type of the output value</typeparam>
	/// <param name="input">The input value passed to <paramref name="func"/></param>
	/// <param name="func">The function to execute</param>
	/// <returns>The value returned by <paramref name="func"/></returns>
	public static TOut Thru<TIn, TOut>(TIn input, Func<TIn, TOut> func)
		=> func(input);

	/// <summary>
	/// A helper function to enable side effects within expressions.
	/// </summary>
	/// <typeparam name="T">The type of the input/output</typeparam>
	/// <param name="input">The input value passed to <paramref name="action"/></param>
	/// <param name="action">The action to execute</param>
	/// <returns>The original input value</returns>
	public static T Tap<T>(T input, Action<T> action)
	{
		action(input);
		return input;
	}

	/// <summary>
	/// A helper function for extracting a value into a variable within an expression.
	/// This is like <see cref="Tap{T}(T, Action{T})"/>,
	/// but it also returns the value returned by <paramref name="func"/>.
	/// </summary>
	/// <typeparam name="TIn">The type of the input value</typeparam>
	/// <typeparam name="TOut">The type of the output value</typeparam>
	/// <param name="input">The input value passed to <paramref name="func"/></param>
	/// <param name="func">The function to execute</param>
	/// <returns>The original input value</returns>
	public static TIn Tap<TIn, TOut>(TIn input, Func<TIn, TOut> func, out TOut output)
	{
		output = func(input);
		return input;
	}
}
