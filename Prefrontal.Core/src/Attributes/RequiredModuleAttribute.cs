namespace Prefrontal.Attributes;

/// <summary>
/// Specifies that this module requires the given module to be present on the <see cref="Agent"/> in order to function.
/// The agent will ensure that the required module is present before configuring and initializing this module.
/// <br/><br/>
/// Example sage:
/// <code language="csharp">
/// public class MyModule : Module
/// {
/// 	[RequiredModule]
/// 	public MyOtherModule OtherModule { get; set; } = null!;
/// }
/// </code>
/// Module dependencies can also be specified in the module's constructor like so:
/// <code language="csharp">
/// public class MyModule(MyOtherModule otherModule) : Module
/// {
/// 	public MyOtherModule OtherModule = otherModule;
/// }
/// </code>
/// </summary>
/// <seealso cref="Agent.GetModulesThatRequire{TModule}"/>
/// <seealso cref="Agent.GetModulesRecursivelyThatRequire{TModule}"/>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class RequiredModuleAttribute : Attribute, IModuleMemberAttribute
{
	
}
