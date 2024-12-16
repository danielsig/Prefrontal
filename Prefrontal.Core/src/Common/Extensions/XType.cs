using System.Text;

namespace Prefrontal.Common.Extensions;


/// <summary> Provides extension methods for <see cref="Type"/>. </summary>
public static class XType
{
	/// <summary>
	/// Converts a <see cref="Type"/> to a verbose string representation.
	/// This works especially well for generic types and arrays.
	/// </summary>
	/// <param name="type">The target type.</param>
	/// <returns>A string representation of the type.</returns>
	public static string ToVerboseString(this Type? type)
	{
		if(type is null)
			return "null";
		
		// primitive types
		int index = (int)Type.GetTypeCode(type);
		if(index > 2 && index < 19 && !type.IsEnum)
			return _PRIMITIVE_NAMES[index];

		// common case
		if(!type.IsArray
		&& !type.IsGenericType
		&& !type.IsNested)
			return type.Name;

		// recursive
		var builder = new StringBuilder(100);
		type.ToVerboseStringRecursive(builder);
		return builder.ToString();
	}

	#region Private
	private static void ToVerboseStringRecursive(this Type type, StringBuilder builder)
	{
		if(type.IsArray)
		{
			Type? bottom = type.GetElementType();
			int index = (int)Type.GetTypeCode(bottom);
			if(index > 2 && index < 19 && !type.IsEnum)
			{
				builder
					.Append(_PRIMITIVE_NAMES[index])
					.Append('[')
					.Append(',', type.GetArrayRank() - 1)
					.Append(']');
				return;
			}

			while(bottom?.IsArray is true)
				bottom = bottom.GetElementType();
			bottom?.ToVerboseStringRecursive(builder);

			Type? subType = type;
			while(subType?.IsArray is true)
			{
				builder
					.Append('[')
					.Append(',', subType.GetArrayRank() - 1)
					.Append(']');
				subType = subType.GetElementType();
			}
		}
		else if(type.IsGenericType)
		{
			Type[] genericArgs = type.GetGenericArguments();
			if(genericArgs.Length > 0)
			{
				int typeIndex = 0;
				type.WriteName(builder, genericArgs, ref typeIndex);
			}
			else type.WriteName(builder);
		}
		else type.WriteName(builder);
	}
	private static void WriteName(this Type type, StringBuilder builder)
	{
		if(type.IsNested && !type.IsGenericParameter)
		{
			type.DeclaringType?.WriteName(builder);
			builder.Append('.');
		}
		builder.Append(GetTypeName(type));
	}
	private static void WriteName(this Type type, StringBuilder builder, Type[] typeArgs, ref int index)
	{
		if(type.IsNested && !type.IsGenericParameter)
		{
			type.DeclaringType?.WriteName(builder, typeArgs, ref index);
			builder.Append('.');
		}
		string name = type.Name;
		int genIndex = name.IndexOf('`');
		if(genIndex > 0)
		{
			builder
				.Append(name, 0, genIndex)
				.Append('<');
			int numTypes = name[genIndex + 1] - '0';
			while(numTypes-- > 0 && index < typeArgs.Length)
			{
				typeArgs[index++].ToVerboseStringRecursive(builder);
				if(numTypes > 0)
					builder.Append(',');
			}
			builder.Append('>');
		}
		else
		{
			genIndex = name.IndexOf('[');
			builder.Append(genIndex < 0 ? name : name.Remove(genIndex));
		}
	}
	private static string GetTypeName(Type type)
	{
		int index = (int)Type.GetTypeCode(type);
		if(index > 2 && index < 19 && !type.IsEnum)
			return _PRIMITIVE_NAMES[index];
		string name = type.Name;
		index = name.IndexOf('[');
		return index < 0 ? name : name.Remove(index);
	}

	private static readonly string[] _PRIMITIVE_NAMES = "empty object null bool char sbyte byte short ushort int uint long ulong float double decimal DateTime ? string".Split(' ');
	#endregion
}
