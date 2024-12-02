# Prefrontal

Run your own LLM powered agents in .NET

Prefrontal is a modular framework that lets you create and run agents composed of modules.

## What it does

You simply instantiate an [Agent](./Prefrontal/Agent.cs) and add different [Modules](./Prefrontal/Modules) to it.

An agent does not do anything by itself, it is a container and mediator between modules.
Each module can depend on other modules on the same agent and interact with them.
Many built-in modules will be made available in the near future.
