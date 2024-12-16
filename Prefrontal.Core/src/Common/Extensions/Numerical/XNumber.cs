#region verbose min/max
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Prefrontal.Common.Extensions;

/// <summary> Provides extension methods for numerical types. </summary>
public static class XNumber
{
	/// <summary>
	/// Returns <paramref name="value"/> if it is <b>less than <paramref name="upperLimit"/></b>,
	/// otherwise returns <paramref name="upperLimit"/>.
	/// This is a shorthand for <c>Math.Min(value, upperLimit)</c>.
	/// </summary>
	/// <remarks>
	/// In case of floating-point values, if either parameter is <see cref="float.NaN"/>,
	/// the result will be <paramref name="upperLimit"/>.
	/// </remarks>
	/// <typeparam name="T">The type of the value.</typeparam>
	/// <param name="value">The value to compare.</param>
	/// <param name="upperLimit">The upper limit of the return value.</param>
	/// <returns>The value or the upper limit.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T OrAtMost<T>(this T value, T upperLimit)
		where T : INumber<T>
		=> value < upperLimit ? value : upperLimit;


	/// <summary>
	/// Returns <paramref name="value"/> if it is <b>greater than <paramref name="lowerLimit"/></b>,
	/// otherwise returns <paramref name="lowerLimit"/>.
	/// This is a shorthand for <c>Math.Max(value, lowerLimit)</c>.
	/// </summary>
	/// <remarks>
	/// In case of floating-point values, if either parameter is <see cref="float.NaN"/>,
	/// the result will be <paramref name="lowerLimit"/>.
	/// </remarks>
	/// <typeparam name="T">The type of the value.</typeparam>
	/// <param name="value">The value to compare.</param>
	/// <param name="lowerLimit">The lower limit of the return value.</param>
	/// <returns>The value or the lower limit.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T OrAtLeast<T>(this T value, T lowerLimit)
		where T : INumber<T>
		=> value > lowerLimit ? value : lowerLimit;

	#endregion
}
