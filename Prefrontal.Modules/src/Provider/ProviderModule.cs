namespace Prefrontal.Modules;

public class ProviderModule<TRequest, T> : Module
{
	private readonly Func<TRequest, T> _factory;
	public ProviderModule(Agent agent, Func<TRequest, T> factory) : base(agent)
	{
		_factory = factory;
		ReceiveSignals(_factory);
	}
}

public class ProviderModule<T> : Module
{
	private readonly Func<T> _factory;
	public ProviderModule(Agent agent, Func<T> factory) : base(agent)
	{
		_factory = factory;
		ReceiveSignals<Request<T>, T>(_ => _factory());
	}
}
