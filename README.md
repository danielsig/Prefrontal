# Prefrontal

### Run your own LLM powered modular agents in .NET Core

> [!WARNING]
> This project is still in its early stages and is not yet ready for production use.

## What is Prefrontal?

Prefrontal is a .NET Core library
that allows you to create, run, and manage modular AI agents.
Each agent can have multiple modules that interact with each other
to perform a specific task or a set of tasks.
The Agent object does not do anything by itself
because it is a container and mediator between modules.
Modules should have a single responsibility
and complex behavior should be achieved
by making modules interact with each other.

### Dependency Injection

Dependency injection is supported in module constructors.</br>
Simply assign the ServiceProvider when instantiating the Agent
and the Agent will use it when you add modules to it.

### Built-in modules

Many built-in modules will be made available in the near future.

## Installation

As this project is still in its early stages,
it is not yet available on NuGet.

## How to use

You simply instantiate an [Agent](./Prefrontal/Agent.cs),
add different [Modules](./Prefrontal/Modules) to it,
and then run it by calling the `Initialize` method.

```csharp
var agent = new Agent
	{
		Name = "MyAgent",
		Description = "My first agent",
	}
	.AddModule<TimerModule>()
	.AddModule<LLMProviderModule>()
	.AddModule<ConsoleChatModule>()
	.Initialize();
```
