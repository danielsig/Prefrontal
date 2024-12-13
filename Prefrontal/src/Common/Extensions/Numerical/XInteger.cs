using System.Numerics;
using System.Runtime.CompilerServices;

namespace Prefrontal.Common.Extensions;

public static class XInteger
{
	#region Int32 specific
	/// <inheritdoc cref="Math.Abs(int)"/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int Abs(this int value)
		=> (value >> 31 | 1) * value;

	#region verbose min/max

	/// <inheritdoc cref="OrAtMost{int}(int, int)"/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int OrAtMost(this int value, int upperLimit)
		=> value < upperLimit ? value : upperLimit;
	

	/// <inheritdoc cref="OrAtLeast{int}(int, int)"/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int OrAtLeast(this int value, int lowerLimit)
		=> value > lowerLimit ? value : lowerLimit;


	#region optimized min/max common cases

	/// <summary>
	/// Returns value if it is less than 0, otherwise returns 0.<br/>
	/// This is a highly optimized alternative to <c>Math.Min(value, 0)</c>
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int OrAtMost0(this int value)
		=> value >> 31 & value;


	/// <summary>
	/// Returns value if it is less than 0, otherwise returns 0.<br/>
	/// This is a highly optimized alternative to <c>Math.Max(value, 0)</c>
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int OrAtLeast0(this int value)
		=> ~value >> 31 & value;


	/// <summary>
	/// Returns value if it is greater than 1, otherwise returns 1.<br/>
	/// This is a highly optimized alternative to <c>Math.Max(value, 1)</c>
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int OrAtLeast1(this int value)
		=> (-(value - (value >> 1)) >> 31 & (value ^ 1)) ^ 1;

	#endregion

	#endregion

	#endregion

	#region Generic

	/// <summary>
	/// Clamps the value between <paramref name="min"/> and <paramref name="max"/>.
	/// </summary>
	/// <typeparam name="T">The type of the value</typeparam>
	/// <param name="value">The value to clamp</param>
	/// <param name="min">The minimum value</param>
	/// <param name="max">The maximum value</param>
	/// <returns>The clamped value</returns>

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T Clamp<T>(this T value, T min, T max)
	where T : IBinaryInteger<T>
		=> value > max
			? max
			: value < min
				? min
				: value;

	#region verbose min/max

	/// <summary>
	/// Returns <paramref name="value"/> if it is <em>less than</em> <paramref name="upperLimit"/>,
	/// otherwise returns <paramref name="upperLimit"/>.
	/// This is a shorthand for <c>Math.Min(value, upperLimit)</c>.
	/// </summary>
	/// <remarks>
	/// In case of floating-point values, if either parameter is <see cref="float.NaN"/>,
	/// the result will be <paramref name="upperLimit"/>.
	/// </remarks>
	/// <typeparam name="T">The type of the value</typeparam>
	/// <param name="value">The value to compare</param>
	/// <param name="upperLimit">The upper limit of the return value</param>
	/// <returns>The value or the upper limit</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T OrAtMost<T>(this T value, T upperLimit)
		where T : INumber<T>
		=> value < upperLimit ? value : upperLimit;


	/// <summary>
	/// Returns <paramref name="value"/> if it is <em>greater than</em> <paramref name="lowerLimit"/>,
	/// otherwise returns <paramref name="lowerLimit"/>.
	/// This is a shorthand for <c>Math.Max(value, lowerLimit)</c>.
	/// </summary>
	/// <remarks>
	/// In case of floating-point values, if either parameter is <see cref="float.NaN"/>,
	/// the result will be <paramref name="lowerLimit"/>.
	/// </remarks>
	/// <typeparam name="T">The type of the value</typeparam>
	/// <param name="value">The value to compare</param>
	/// <param name="lowerLimit">The lower limit of the return value</param>
	/// <returns>The value or the lower limit</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T OrAtLeast<T>(this T value, T lowerLimit)
		where T : INumber<T>
		=> value > lowerLimit ? value : lowerLimit;

	#endregion

	#endregion
}
