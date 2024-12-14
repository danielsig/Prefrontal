namespace Prefrontal.Common;

/// <summary> Empty service provider. </summary>
public sealed class EmptyServiceProvider : IServiceProvider
{
	/// <summary> Singleton instance. </summary>
	public static EmptyServiceProvider Instance { get; } = new();

	/// <inheritdoc />
	public object? GetService(Type serviceType) => null;
}
