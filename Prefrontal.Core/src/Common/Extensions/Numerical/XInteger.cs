using System.Numerics;
using System.Runtime.CompilerServices;

namespace Prefrontal.Common.Extensions;

/// <summary> Provides extension methods for integer types. </summary>
public static class XInteger
{
	#region Int32 specific
	/// <inheritdoc cref="Math.Abs(int)"/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int Abs(this int value)
		=> (value >> 31 | 1) * value;

	#region verbose min/max

	/// <inheritdoc cref="XNumber.OrAtMost{int}(int, int)"/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int OrAtMost(this int value, int upperLimit)
		=> value < upperLimit ? value : upperLimit;
	

	/// <inheritdoc cref="XNumber.OrAtLeast{int}(int, int)"/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int OrAtLeast(this int value, int lowerLimit)
		=> value > lowerLimit ? value : lowerLimit;


	#region optimized min/max common cases

	/// <summary>
	/// Returns value if it is <b>less than</b> 0, otherwise returns 0.<br/>
	/// This is a highly optimized alternative to <c>Math.Min(value, 0)</c>
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int OrAtMost0(this int value)
		=> value >> 31 & value;


	/// <summary>
	/// Returns value if it is <b>less than</b> 0, otherwise returns 0.<br/>
	/// This is a highly optimized alternative to <c>Math.Max(value, 0)</c>
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int OrAtLeast0(this int value)
		=> ~value >> 31 & value;


	/// <summary>
	/// Returns value if it is <b>greater than</b> 1, otherwise returns 1.<br/>
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
	/// Clamps the value between <paramref name="min"/> and <paramref name="max"/> (inclusive).
	/// </summary>
	/// <typeparam name="T">The type of the value.</typeparam>
	/// <param name="value">The value to clamp.</param>
	/// <param name="min">The minimum value.</param>
	/// <param name="max">The maximum value.</param>
	/// <returns>The clamped value.</returns>

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T Clamp<T>(this T value, T min, T max)
	where T : IBinaryInteger<T>
		=> value > max
			? max
			: value < min
				? min
				: value;

	#endregion
}
