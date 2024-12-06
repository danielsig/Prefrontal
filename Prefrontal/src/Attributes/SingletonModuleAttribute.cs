namespace Prefrontal.Attributes;

/// <summary>
/// Specifies that there can only be one instance of this module type per <see cref="Agent"/>.
/// <br/>
/// Example:
/// <code>
/// [SingletonModule]
/// public class MySingletonModule : Module
/// {
/// 	// ...
/// }
/// </code>
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class SingletonModuleAttribute : Attribute, IModuleAttribute
{

}
