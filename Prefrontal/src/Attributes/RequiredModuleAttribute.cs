namespace Prefrontal.Attributes;

/// <summary>
/// Specifies that this module requires the given module to be present on the <see cref="Agent"/> in order to function.
/// The agent will ensure that the required module is present before configuring and initializing this module.
/// Module dependencies can also be specified in each module's constructor.
/// <br/>
/// Example:
/// <code language="csharp">
/// public class MyModule : Module
/// {
/// 	[RequiredModule]
/// 	public MyOtherModule OtherModule { get; set; } = null!;
/// }
/// </code>
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class RequiredModuleAttribute : Attribute, IModuleMemberAttribute
{
	
}
