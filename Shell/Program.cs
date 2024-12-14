using Prefrontal;

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.InputEncoding = System.Text.Encoding.UTF8;

Console.WriteLine(
	new Agent
	{
		Name = "foobar",
		Description = "A placeholder agent with a seriously long description that needs to be wrapped around",
	}
	.AddModule<FooModule>()
	.AddModule<BarModule>()
	.ToStringPretty()
);

Console.WriteLine("Press any key to continue...");
Console.ReadKey();




internal class BarModule : Module
{
	public override string ToString()
	{
		return "The Bar around the corner";
	}
}

internal class FooModule : Module
{
	public override string ToString()
	{
		return "The Foo\nthat makes the world go round";
	}
}
