using System.Numerics;
using System.Runtime.CompilerServices;

namespace Prefrontal.Common.Extensions;

/// <summary> Provides extension methods for floating point types. </summary>
public static class XFloatingPoint
{
	/// <summary>
	/// Clamps the value between <paramref name="min"/> and <paramref name="max"/>.
	/// </summary>
	/// <typeparam name="T">The type of the value.</typeparam>
	/// <param name="value">The value to clamp.</param>
	/// <param name="min">The minimum value.</param>
	/// <param name="max">The maximum value.</param>
	/// <returns>The clamped value.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T Clamp<T>(this T value, T min, T max)
	where T : IFloatingPoint<T>
		=> T.Clamp(value, min, max);
}
