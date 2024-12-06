# Prefrontal

### Run your own LLM powered modular agents in .NET Core

> [!WARNING]
> This project is still in its early stages and is not yet ready for production use.

Check out the [Prefrontal API Docs](https://danielsig.github.io/Prefrontal/api/Prefrontal.html).

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

You simply instantiate an [Agent](https://danielsig.github.io/Prefrontal/api/Prefrontal.Agent.html),
add various [Modules](https://danielsig.github.io/Prefrontal/api/Prefrontal.Modules.html)
to it via [AddModule&lt;T&gt;()](https://danielsig.github.io/Prefrontal/api/Prefrontal.Agent.html#Prefrontal_Agent_AddModule__1_System_Action___0__)
and then run it by calling
[Initialize()](https://danielsig.github.io/Prefrontal/api/Prefrontal.Agent.html#Prefrontal_Agent_Initialize).

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
