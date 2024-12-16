namespace Prefrontal.Common.Extensions;

/// <summary> Extension methods for <see cref="string"/>. </summary>
public static class XString
{
	/// <summary>
	/// Returns the string itself if it's not null nor empty, otherwise returns null.<br/>
	/// Handy for using <see href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/null-coalescing-operator">??</see> operator to provide a default value for a string.
	/// </summary>
	public static string? NullIfEmpty(this string? value)
		=> string.IsNullOrEmpty(value) ? null : value;
	/// <summary>
	/// Returns the string itself if it contains any non-whitespace characters, otherwise returns null.<br/>
	/// Handy for using <see href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/null-coalescing-operator">??</see> operator to provide a default value for a string.
	/// </summary>
	public static string? NullIfWhiteSpace(this string? value)
		=> string.IsNullOrWhiteSpace(value) ? null : value;

	/// <summary>
	/// Splits a string into lines (by <paramref name="lineEnding"/>)
	/// and wraps the lines at <paramref name="maxLineLength"/>.
	/// This method makes sure that words are not split between lines.
	/// The lines are trimmed of leading and trailing whitespace
	/// and then prefixed with <paramref name="linePrefix"/>.
	/// </summary>
	/// <param name="value">The string to split and wrap.</param>
	/// <param name="maxLineLength">The maximum length of a line.</param>
	/// <param name="lineEnding">The line ending to split the string by.</param>
	/// <param name="linePrefix">The prefix to add to each line.</param>
	/// <returns>An enumerable of lines.</returns>
	/// <exception cref="ArgumentException">If <paramref name="value"/> is null.</exception>
	/// <exception cref="ArgumentException">If <paramref name="lineEnding"/> is null or empty.</exception>
	/// <exception cref="ArgumentOutOfRangeException">If <paramref name="maxLineLength"/> is less than or equal to zero.</exception>
	public static IEnumerable<string> SplitAndWrapLines(
		this string value,
		int maxLineLength,
		string lineEnding = "\n",
		string linePrefix = ""
	)
	{
		if(value is null)
			throw new ArgumentException("Value must not be null", nameof(lineEnding));
		if(string.IsNullOrEmpty(lineEnding))
			throw new ArgumentException("The line ending must not be null nor empty.", nameof(lineEnding));
		if(maxLineLength <= 0)
			throw new ArgumentOutOfRangeException(nameof(maxLineLength), "The line length must be greater than zero.");

		if(value.Length <= maxLineLength)
		{
			if(value.Contains(lineEnding))
				foreach(string line in value.Split(lineEnding))
					yield return linePrefix + line;
			else
				yield return linePrefix + value;
			yield break;
		}

		for(int i = 0; i < value.Length;)
		{
			// trim the line's leading whitespace
			while(i < value.Length && char.IsWhiteSpace(value[i]))
				i++;
			
			// get the line's length
			int length = maxLineLength.OrAtMost(value.Length - i);
			if(value.IndexOf(lineEnding, i, length) is int nextNewlineIndex and > 0)
				length = length.OrAtMost(nextNewlineIndex - i);

			// break if no more lines
			if(length == 0)
				break;

			// check if this line ends with non-whitespace
			// and next line starts with non-whitespace
			if(i + length < value.Length
			&& !char.IsWhiteSpace(value[i + length - 1])
			&& !char.IsWhiteSpace(value[i + length]))
				// if so, we're in the middle of a word and need to wrap it into the next line
				for(int newLength = length - 1; newLength > 0; newLength--)
					if(char.IsWhiteSpace(value[i + newLength]))
					{
						// found the start of this line's last word
						length = newLength;
						break;
					}

			// set the index of the next line
			// before trimming the trailing whitespace of this line
			var nextLineIndex = i + length;

			// trim the line's trailing whitespace
			while(i + length <= value.Length && char.IsWhiteSpace(value[i + length - 1]))
				length--;

			// break if no more lines
			if(length == 0)
				break;

			// fetch the line
			yield return linePrefix + value[i..(i + length)];

			// go to the next line
			i = nextLineIndex;
		}
	}

	/// <summary>
	/// Wraps the string inside a box with the given dimensions and style.<br/>
	/// Example:
	/// <code language="csharp">
	/// var wrapped = "Hello world!".WrapInsideBox(9);
	/// Console.WriteLine(wrapped);
	/// // Output:
	/// // ╭────────╮
	/// // │ Hello  │
	/// // │ world! │
	/// // ╰────────╯
	/// </code>
	/// </summary>
	/// <param name="value">The string to wrap inside a box.</param>
	/// <param name="maxWidth">The maximum total width of the box (including the border).</param>
	/// <param name="minWidth">The minimum total width of the box (including the border).</param>
	/// <param name="horizontalPadding">The number of spaces to pad the content with on the left and right within the box.</param>
	/// <param name="verticalPadding">The number of empty lines to add above and below the content within the box.</param>
	/// <param name="linePrefix">The string to prepend to each line. Useful for indentation.</param>
	/// <param name="lineEnding">The newline character used (you'll most likely not need to edit this).</param>
	/// <param name="horizontal">The character to use for the horizontal border.</param>
	/// <param name="vertical">The character to use for the vertical border.</param>
	/// <param name="topLeft">The character to use for the top left corner of the box.</param>
	/// <param name="topRight">The character to use for the top right corner of the box.</param>
	/// <param name="bottomLeft">The character to use for the bottom left corner of the box.</param>
	/// <param name="bottomRight">The character to use for the bottom right corner of the box.</param>
	/// <returns>The string wrapped inside a box.</returns>
	/// <exception cref="ArgumentException">If <paramref name="value"/> is null.</exception>
	public static string WrapInsideBox(
		this string value,
		int maxWidth,
		int minWidth = 0,
		int horizontalPadding = 1,
		int verticalPadding = 0,
		string linePrefix = "",
		string lineEnding = "\n",
		char horizontal = '─',
		char vertical = '│',
		char topLeft = '╭',
		char topRight = '╮',
		char bottomLeft = '╰',
		char bottomRight = '╯'
	)
	{
		ArgumentNullException.ThrowIfNull(value, nameof(value));

		// apply constraints
		maxWidth = maxWidth.OrAtLeast(2 + horizontalPadding.OrAtLeast0() * 2);
		horizontalPadding = horizontalPadding.Clamp(0, (maxWidth / 2) - 2);
		minWidth = minWidth.OrAtLeast(horizontalPadding * 2 + 2);
		verticalPadding = verticalPadding.OrAtLeast0();
		lineEnding ??= "\n";
		linePrefix ??= "";

		// calculate the box's dimensions
		var paddingPlusBorderWidth = horizontalPadding * 2 + 2;
		var maxLineLength = maxWidth - paddingPlusBorderWidth;
		var lines = value.SplitAndWrapLines(maxLineLength, lineEnding).ToList(); // split value into wrapped lines
		maxLineLength = lines.Max(l => l.Length).OrAtMost(maxLineLength);
		var width = (maxLineLength + paddingPlusBorderWidth)
			.Clamp(
				minWidth,
				maxWidth.OrAtLeast(minWidth)
			);
		maxLineLength = width - paddingPlusBorderWidth;

		// construct the box, line by line
		var paddingString = new string(' ', horizontalPadding);
		var paddingLine = $"{linePrefix}{vertical}{new string(' ', width - 2)}{vertical}";
		var paddingLines = Enumerable.Repeat(paddingLine, verticalPadding);
		var firstLine = $"{linePrefix}{topLeft}{new string(horizontal, width - 2)}{topRight}";
		var lastLine = $"{linePrefix}{bottomLeft}{new string(horizontal, width - 2)}{bottomRight}";
		var contentLines = lines.Select(l =>
			$"{linePrefix}{vertical}{paddingString}{l.PadRight(maxLineLength)}{paddingString}{vertical}"
		);

		// return the box as a string
		return string.Join(
			lineEnding,
			[
				firstLine,
				..paddingLines,
				..contentLines,
				..paddingLines,
				lastLine,
			]
		);
	}

	#region Before/After

	/// <summary>
	/// Returns the left hand side of the <em>first occurrence</em> of <paramref name="delimiter"/> in the string.
	/// If the delimiter is not found, the whole string is returned unless <paramref name="returnWholeStringOnDefault"/> set to false.
	/// </summary>
	/// <param name="str">The string to search in.</param>
	/// <param name="delimiter">The delimiter to search for.</param>
	/// <param name="startIndex">The index in the string to start searching from.</param>
	/// <param name="returnWholeStringOnDefault">Whether to return the whole string if the delimiter is not found (true), or an empty string (false).</param>
	/// <returns>The left hand side of the delimiter if found, otherwise the whole string or an empty string.</returns>
	/// <exception cref="NullReferenceException">If <paramref name="str"/> is null.</exception>
	public static string Before(this string str, string delimiter, int startIndex, bool returnWholeStringOnDefault = true)
	{
		int index = str.IndexOf(delimiter, startIndex, StringComparison.InvariantCulture);
		return index < 0 ? (returnWholeStringOnDefault ? str : "") : str.Remove(index);
	}
	/// <inheritdoc cref="Before(string, string, int, bool)"/>
	public static string Before(this string str, char delimiter, int startIndex, bool returnWholeStringOnDefault = true)
	{
		int index = str.IndexOf(delimiter, startIndex);
		return index < 0 ? (returnWholeStringOnDefault ? str : "") : str.Remove(index);
	}
	/// <inheritdoc cref="Before(string, string, int, bool)"/>
	public static string Before(this string str, string delimiter, bool returnWholeStringOnDefault = true)
	{
		int index = str.IndexOf(delimiter, StringComparison.InvariantCulture);
		return index < 0 ? (returnWholeStringOnDefault ? str : "") : str.Remove(index);
	}
	/// <inheritdoc cref="Before(string, string, int, bool)"/>
	public static string Before(this string str, char delimiter, bool returnWholeStringOnDefault = true)
	{
		int index = str.IndexOf(delimiter);
		return index < 0 ? (returnWholeStringOnDefault ? str : "") : str.Remove(index);
	}


	/// <summary>
	/// Returns the right hand side of the <em>first occurrence</em> of <paramref name="delimiter"/> in the string.
	/// If the delimiter is not found, the whole string is returned unless <paramref name="returnWholeStringOnDefault"/> set to false.
	/// </summary>
	/// <inheritdoc cref="Before"/>
	public static string After(this string str, string delimiter, int startIndex, bool returnWholeStringOnDefault = true)
	{
		int index = str.IndexOf(delimiter, startIndex, StringComparison.InvariantCulture);
		return index < 0 ? (returnWholeStringOnDefault ? str : "") : str.Remove(0, index + delimiter.Length);
	}
	/// <inheritdoc cref="After(string, string, int, bool)"/>
	public static string After(this string str, char delimiter, int startIndex, bool returnWholeStringOnDefault = true)
	{
		int index = str.IndexOf(delimiter, startIndex);
		return index < 0 ? (returnWholeStringOnDefault ? str : "") : str.Remove(0, index + 1);
	}
	/// <inheritdoc cref="After(string, string, int, bool)"/>
	public static string After(this string str, string delimiter, bool returnWholeStringOnDefault = true)
	{
		int index = str.IndexOf(delimiter, StringComparison.InvariantCulture);
		return index < 0 ? (returnWholeStringOnDefault ? str : "") : str.Remove(0, index + delimiter.Length);
	}
	/// <inheritdoc cref="After(string, string, int, bool)"/>
	public static string After(this string str, char delimiter, bool returnWholeStringOnDefault = true)
	{
		int index = str.IndexOf(delimiter);
		return index < 0 ? (returnWholeStringOnDefault ? str : "") : str.Remove(0, index + 1);
	}


	/// <summary>
	/// Returns the left hand side of the <em>last occurrence</em> of <paramref name="delimiter"/> in the string.
	/// If the delimiter is not found, the whole string is returned unless <paramref name="returnWholeStringOnDefault"/> set to false.
	/// </summary>
	/// <inheritdoc cref="Before"/>
	public static string BeforeLast(this string str, string delimiter, int startIndex, bool returnWholeStringOnDefault = true)
	{
		int index = str.LastIndexOf(delimiter, startIndex, StringComparison.InvariantCulture);
		return index < 0 ? (returnWholeStringOnDefault ? str : "") : str.Remove(index);
	}
	/// <inheritdoc cref="BeforeLast(string, string, int, bool)"/>
	public static string BeforeLast(this string str, char delimiter, int startIndex, bool returnWholeStringOnDefault = true)
	{
		int index = str.LastIndexOf(delimiter, startIndex);
		return index < 0 ? (returnWholeStringOnDefault ? str : "") : str.Remove(index);
	}
	/// <inheritdoc cref="BeforeLast(string, string, int, bool)"/>
	public static string BeforeLast(this string str, string delimiter, bool returnWholeStringOnDefault = true)
	{
		int index = str.LastIndexOf(delimiter, StringComparison.InvariantCulture);
		return index < 0 ? (returnWholeStringOnDefault ? str : "") : str.Remove(index);
	}
	/// <inheritdoc cref="BeforeLast(string, string, int, bool)"/>
	public static string BeforeLast(this string str, char delimiter, bool returnWholeStringOnDefault = true)
	{
		int index = str.LastIndexOf(delimiter);
		return index < 0 ? (returnWholeStringOnDefault ? str : "") : str.Remove(index);
	}


	/// <summary>
	/// Returns the right hand side of the <em>last occurrence</em> of <paramref name="delimiter"/> in the string.
	/// If the delimiter is not found, the whole string is returned unless <paramref name="returnWholeStringOnDefault"/> set to false.
	/// </summary>
	/// <inheritdoc cref="Before"/>
	public static string AfterLast(this string str, string delimiter, int startIndex, bool returnWholeStringOnDefault = true)
	{
		int index = str.LastIndexOf(delimiter, startIndex, StringComparison.InvariantCulture);
		return index < 0 ? (returnWholeStringOnDefault ? str : "") : str.Remove(0, index + delimiter.Length);
	}
	/// <inheritdoc cref="AfterLast(string, string, int, bool)"/>
	public static string AfterLast(this string str, char delimiter, int startIndex, bool returnWholeStringOnDefault = true)
	{
		int index = str.LastIndexOf(delimiter, startIndex);
		return index < 0 ? (returnWholeStringOnDefault ? str : "") : str.Remove(0, index + 1);
	}
	/// <inheritdoc cref="AfterLast(string, string, int, bool)"/>
	public static string AfterLast(this string str, string delimiter, bool returnWholeStringOnDefault = true)
	{
		int index = str.LastIndexOf(delimiter, StringComparison.InvariantCulture);
		return index < 0 ? (returnWholeStringOnDefault ? str : "") : str.Remove(0, index + delimiter.Length);
	}
	/// <inheritdoc cref="AfterLast(string, string, int, bool)"/>
	public static string AfterLast(this string str, char delimiter, bool returnWholeStringOnDefault = true)
	{
		int index = str.LastIndexOf(delimiter);
		return index < 0 ? (returnWholeStringOnDefault ? str : "") : str.Remove(0, index + 1);
	}

	#endregion

	#region Substring methods

	/// <summary>
	/// Just like <c>string[..maxLength]</c> except it doesn't throw an exception
	/// when <paramref name="maxLength"/> is greater than the string's length
	/// and instead returns the original string.
	/// </summary>
	/// <param name="input">The input string.</param>
	/// <param name="maxLength">The maximum length of the returned substring.</param>
	/// <returns><paramref name="input"/> with length capped at <paramref name="maxLength"/>.</returns>
	/// <exception cref="NullReferenceException">If <paramref name="input"/> is null.</exception>
	/// <exception cref="ArgumentOutOfRangeException">If <paramref name="maxLength"/> is less than zero.</exception>
	public static string WithMaxLength(this string input, int maxLength)
		=> input.Length <= maxLength
			? input
			: input[..maxLength];


	/// <summary>
	/// A version of <see cref="WithMaxLength(string, int)"/>
	/// that includes the given <paramref name="suffix"/> at the end of the resulting string
	/// when <paramref name="input"/> exceeds <paramref name="maxLength"/>.<br/>
	/// Note that the returned string will still be at most <paramref name="maxLength"/> characters long.
	/// </summary>
	/// <inheritdoc cref="WithMaxLength(string, int)"/>
	/// <param name="suffix">The suffix of the resulting string if <paramref name="input"/> exceeds <paramref name="maxLength"/>.</param>
	public static string WithMaxLengthSuffixed(this string input, int maxLength, string suffix = "…")
		=> input.Length <= maxLength
			? input
			: string.IsNullOrEmpty(suffix) || suffix.Length >= maxLength
				? input[..maxLength]
				: input[..(maxLength - suffix.Length)] + suffix;

	#endregion
}
