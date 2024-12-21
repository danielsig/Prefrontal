﻿using Prefrontal;
using Prefrontal.Common.Extensions;
using Prefrontal.Common.Extensions.Async;

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.InputEncoding = System.Text.Encoding.UTF8;

Console.WriteLine(
	new Agent
	{
		Name = "foobar",
		Description = "A placeholder agent with a seriously long description that needs to be wrapped around",
	}
	.AddModule<Foo2Module>()
	.AddModule<Bar2Module>()
	.Initialize()
	.ToStringPretty()
);

var agent = new Agent
	{
		Name = "foobar",
		Description = "Brand new agent",
	}
	.AddModule<BarModule>()
	.AddModule<FooModule>()
	.SetSignalProcessingOrder<string>(a =>
	[
		a.GetModule<FooModule>(),
		a.GetModule<BarModule>(),
	])
	.Initialize();

Console.WriteLine();
Console.WriteLine(agent.ToStringPretty());
Console.WriteLine();

agent.Description = "A placeholder agent with a seriously long description that needs to be wrapped around";
await agent.Initialization;

Console.WriteLine();
Console.WriteLine(agent.ToStringPretty());
Console.WriteLine();

var response = await agent
	.SendSignalAsync<string, object>("!olleH") // cSpell: words olleH
	.ToListAsync()
	.Then(l => l.Join());
Console.WriteLine(response);

agent.Dispose();

agent.Description = "R.I.P.";
Console.WriteLine();
Console.WriteLine(agent.ToStringPretty());
Console.WriteLine();

Console.WriteLine("Press any key to continue...");
Console.ReadKey();





internal class FooModule : Module
{
	public override string ToString()
	{
		return "The Foo module intercepts string signals and reverses them before passing them on";
	}

	protected override async Task InitializeAsync()
	{
		InterceptSignalsAsync<string, int>(s =>
		{
			var reversedSignal = s.Signal.Reverse().Join("");
			Console.WriteLine($"Foo intercepted signal: {s.Signal} and reversed it to {reversedSignal}");
			return s.Next(reversedSignal)
				.Select(r => r * 2)
				.Append(-1);
		});
		await Task.Delay(1000);
	}
}

internal class BarModule : Module
{
	public override string ToString()
	{
		return "The Bar module receives string signals\nand prints them to the console";
	}

	protected override async Task InitializeAsync()
	{
		await Task.Delay(2000);
		ReceiveSignals((string signal) =>
		{
			Console.WriteLine($"Bar received signal: {signal}");
			return 44;
		});
	}
}


public class Foo2Module : Module
{
	public override string ToString() => "Foo with a long description that needs to be wrapped around";
}

public class Bar2Module : Module
{
}
