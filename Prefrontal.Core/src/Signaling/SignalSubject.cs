namespace Prefrontal.Signaling;

// TODO: need to change this so that it can handle async signal processing and a better way to handle signal interception (cancel/replace)
internal record SignalSubject<TSignal>() : IObservable<TSignal>, IObserver<TSignal>, IDisposable
{
	private readonly Lock _gate = new();
	private SignalSubscription[] _subscriptions = [];
	private List<Module> _preferredOrder = [];

	public void SetSignalProcessingOrder(IEnumerable<Module> modules)
	{
		ArgumentNullException.ThrowIfNull(modules);
		lock(_gate)
		{
			_preferredOrder = [.. modules];
			_subscriptions = _preferredOrder
				.Distinct()
				.SelectMany(m => m is not null ? _subscriptions.Where(o => o.Module == m) : [])
				.Concat(_subscriptions.Where(o
					=> o.Module is null
					|| !_preferredOrder.Contains(o.Module)
				))
				.ToArray();
		}
	}

	public IDisposable Subscribe(IObserver<TSignal> observer)
	{
		ArgumentNullException.ThrowIfNull(observer);
		return Subscribe(new SignalObserverSubscription(this, observer));
	}
	internal SignalSubscription Subscribe(SignalSubscription subscription)
	{
		lock(_gate)
			_subscriptions = [.. _subscriptions, subscription];

		if(subscription.Module is not null
		&& _preferredOrder.Count > 0
		&& _preferredOrder.Contains(subscription.Module))
			SetSignalProcessingOrder(_preferredOrder); // re-order subscriptions
		
		return subscription;
	}

	internal abstract record SignalSubscription
	(
		SignalSubject<TSignal> Subject,
		Module? Module = null
	) : IDisposable
	{
		public void Dispose()
		{
			lock(Subject._gate)
			{
				switch(Subject._subscriptions.Length)
				{
					case 0:
						return;
					case 1 when Subject._subscriptions[0] == this:
						Subject._subscriptions = [];
						if(Module is not null)
							Subject._preferredOrder = [];
						return;
					default:
						Subject._subscriptions = Subject._subscriptions
							.Where(o => o != this)
							.ToArray();
						if(Module is not null)
							Subject._preferredOrder.Remove(Module);
						return;
				}
			}
		}
	}
	internal sealed record SignalObserverSubscription
	(
		SignalSubject<TSignal> Subject,
		IObserver<TSignal> Observer,
		Module? Module = null
	) : SignalSubscription(Subject, Module);
	internal sealed record SignalReceiverSubscription
	(
		SignalSubject<TSignal> Subject,
		Action<TSignal> Receiver,
		Module? Module = null
	) : SignalSubscription(Subject, Module);
	internal sealed record SignalInterceptorSubscription
	(
		SignalSubject<TSignal> Subject,
		Func<TSignal, Intercept<TSignal>> Interceptor,
		Module? Module = null
	) : SignalSubscription(Subject, Module);
	internal sealed record AsyncSignalReceiverSubscription
	(
		SignalSubject<TSignal> Subject,
		Func<TSignal, Task> Receiver,
		Module? Module = null
	) : SignalSubscription(Subject, Module);
	internal sealed record AsyncSignalInterceptorSubscription
	(
		SignalSubject<TSignal> Subject,
		Func<TSignal, Task<Intercept<TSignal>>> Interceptor,
		Module? Module = null
	) : SignalSubscription(Subject, Module);

	public void Dispose()
	{
		SignalSubscription[] subscriptions;
		lock(_gate)
		{
			if(_preferredOrder.Count > 0)
				_preferredOrder = [];
			if(_subscriptions.Length == 0)
				return;
			subscriptions = _subscriptions;
			_subscriptions = [];
		}
		using var errors = new ExceptionAggregator();
		foreach(var sub in subscriptions)
			if(sub is SignalObserverSubscription { Observer: var observer })
				errors.Try(observer.OnCompleted);
	}

	public void OnNext(TSignal signal)
		=> Task.Run(() => OnNextAsync(signal));
	public async Task OnNextAsync(TSignal signal)
	{
		SignalSubscription[] subscriptions;
		lock(_gate)
		{
			if(_subscriptions.Length == 0)
				return;
			subscriptions = _subscriptions;
			_subscriptions = [];
		}
		using var errors = new ExceptionAggregator();
		foreach(var sub in subscriptions)
			try
			{
				Intercept<TSignal> intercept;
				switch(sub)
				{
					case { Module.Agent: null }:
						continue; // skip if the module is not attached to an agent
					case SignalObserverSubscription { Observer: var observer }:
						observer.OnNext(signal);
						continue;
					case SignalReceiverSubscription { Receiver: var receiver }:
						receiver(signal);
						continue;
					case SignalInterceptorSubscription { Interceptor: var interceptor }:
						intercept = interceptor(signal);
						break;
					case AsyncSignalReceiverSubscription { Receiver: var receiver }:
						await receiver(signal);
						continue;
					case AsyncSignalInterceptorSubscription { Interceptor: var interceptor }:
						intercept = await interceptor(signal);
						break;
					default:
						throw new NotSupportedException($"Unknown signal subscription type: {sub.GetType().ToVerboseString()}");
				}
				if(intercept.ShouldStopProcessing)
					return;
				signal = intercept.Signal;
			}
			catch(Exception ex)
			{
				errors.Add(ex);
			}
	}

	public void OnError(Exception error)
	{
		SignalSubscription[] subscriptions;
		lock(_gate)
		{
			if(_subscriptions.Length == 0)
				return;
			subscriptions = _subscriptions;
			_subscriptions = [];
		}
		using var errors = new ExceptionAggregator();
		foreach(var sub in subscriptions)
			if(sub is SignalObserverSubscription { Observer: var observer })
				errors.Try(() => observer.OnError(error));
	}

	public void OnCompleted()
	{
		SignalSubscription[] subscriptions;
		lock(_gate)
		{
			if(_subscriptions.Length == 0)
				return;
			subscriptions = _subscriptions;
			_subscriptions = [];
		}
		using var errors = new ExceptionAggregator();
		foreach(var sub in subscriptions)
			if(sub is SignalObserverSubscription { Observer: var observer })
				errors.Try(observer.OnCompleted);
	}
}
