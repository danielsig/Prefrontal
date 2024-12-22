namespace Prefrontal.Signaling;

internal abstract class Signaler : IDisposable
{
	public abstract void SetSignalProcessingOrder(Module[] modules);
	public abstract void RemoveModule(Module module);
	public abstract void Dispose();
}

internal sealed class Signaler<TSignal> : Signaler, IObservable<TSignal>
{
	/// <summary>
	/// The lock object for reassigning <c>_processors</c> and <c>_preferredOrder</c>.
	/// <list type="bullet">
	/// 	<item>
	/// 		All changes to <c>_processors</c> and <c>_preferredOrder</c>
	/// 		creates a new array (for thread safety).
	/// 	</item>
	/// 	<item>
	/// 		Reading from <c>_processors</c> and <c>_preferredOrder</c>
	/// 		does not require locking. We simply make sure to assign them
	/// 		to a local variable before using them.
	/// 	</item>
	/// </list>
	/// </summary>
	private readonly Lock _gate = new();

	/// <summary> All signal processors currently subscribed to this signaler. </summary>
	private Processor[] _processors = [];

	/// <summary> The preferred order of signal processing. </summary>
	private Module[] _preferredOrder = [];

	public override void SetSignalProcessingOrder(Module[] modules)
	{
		ArgumentNullException.ThrowIfNull(modules);
		lock(_gate)
		{
			if(_preferredOrder != modules) // common case optimization
			{
				_preferredOrder = [..modules.WhereNotNull().Distinct()];
				foreach(var module in _preferredOrder)
					module._signalers.Add(this);
			}
			_processors = _preferredOrder
				.SelectMany(m => m is not null
					? _processors.Where(o => o.Module == m)
					: []
				)
				.Concat(_processors.Where(o
					=> o.Module is null
					|| !_preferredOrder.Contains(o.Module)
				))
				.ToArray();
		}
	}
	public override void RemoveModule(Module module)
	{
		ArgumentNullException.ThrowIfNull(module);
		lock(_gate)
		{
			_processors = _processors
				.Where(o => o.Module != module)
				.ToArray();
			_preferredOrder = _preferredOrder
				.Where(m => m != module)
				.ToArray();
		}
	}
	public override void Dispose()
	{
		lock(_gate)
		{
			_processors = [];
			_preferredOrder = [];
		}
	}
	public IDisposable Subscribe(IObserver<TSignal> observer)
	{
		ArgumentNullException.ThrowIfNull(observer);
		return Subscribe(new ObserverProcessor(this, observer));
	}
	internal IDisposable Subscribe(Processor processor)
	{
		ArgumentNullException.ThrowIfNull(processor);
		if(processor.Signaler != this)
			throw new ArgumentException("Processor does not belong to this signaler.", nameof(processor));

		lock(_gate)
		{
			if(_processors.Contains(processor))
				return processor;
			_processors = [.. _processors, processor];
		}

		if(processor.Module is not null
		&& _preferredOrder.Length > 0
		&& _preferredOrder.Contains(processor.Module))
			SetSignalProcessingOrder(_preferredOrder); // re-order subscriptions

		return processor;
	}

	public async Task SendSignalAsync(TSignal signal)
	{
		await foreach(var _ in SendSignalAsync<object?>(signal)){}
	}


	public IAsyncEnumerable<TResponse> SendSignalAsync<TResponse>(TSignal signal)
	{
		ArgumentNullException.ThrowIfNull(signal);

		if(_processors.Length == 0)
			return signal is TResponse response
				? AsyncEnumerable.FromValue(response)
				: AsyncEnumerable.Empty<TResponse>();

		var processors = _processors;
		int index = 0;
		Func<TSignal, IAsyncEnumerable<TResponse>> next = null!;
		next = signalInTransit =>
		{
			if(index >= processors.Length)
				return AsyncEnumerable.Empty<TResponse>();
			return processors[index++]
				.OnNext(signalInTransit, next)!;
		};

		return next(signal);
	}

	internal abstract record Processor
	(
		Signaler<TSignal> Signaler,
		Module? Module = null
	) : IDisposable
	{
		public void Dispose()
		{
			lock(Signaler._gate)
			{
				int indexOfThis = Array.IndexOf(Signaler._processors, this);
				if(indexOfThis >= 0)
				{
					Processor[] newProcessors = new Processor[Signaler._processors.Length - 1];
					Array.Copy(Signaler._processors, 0, newProcessors, 0, indexOfThis);
					Array.Copy(Signaler._processors, indexOfThis + 1, newProcessors, indexOfThis, newProcessors.Length - indexOfThis);
					Signaler._processors = newProcessors;
				}
			}
		}
		public abstract IAsyncEnumerable<TResponse> OnNext<TResponse>(
			TSignal signal,
			Func<TSignal, IAsyncEnumerable<TResponse>> next
		);
	}

	internal sealed record ObserverProcessor
	(
		Signaler<TSignal> Signaler,
		IObserver<TSignal> Observer,
		Module? Module = null
	) : Processor(Signaler, Module)
	{
		public override IAsyncEnumerable<TResponse> OnNext<TResponse>(
			TSignal signal,
			Func<TSignal, IAsyncEnumerable<TResponse>> next
		)
		{
			Observer.OnNext(signal);
			return next(signal)!;
		}
	}

	internal sealed record ReceiverProcessor<TResult>
	(
		Signaler<TSignal> Signaler,
		Func<TSignal, TResult> Receiver,
		Module? Module = null
	) : Processor(Signaler, Module)
	{
		public override async IAsyncEnumerable<TResponse> OnNext<TResponse>(
			TSignal signal,
			Func<TSignal, IAsyncEnumerable<TResponse>> next
		)
		{
			if(Receiver(signal) is TResponse response)
				yield return response;
			await foreach(var value in next(signal))
				yield return value;
		}
	}

	internal sealed record ReceiverProcessor
	(
		Signaler<TSignal> Signaler,
		Action<TSignal> Receiver,
		Module? Module = null
	) : Processor(Signaler, Module)
	{
		public override IAsyncEnumerable<TResponse> OnNext<TResponse>(
			TSignal signal,
			Func<TSignal, IAsyncEnumerable<TResponse>> next
		)
		{
			Receiver(signal);
			return next(signal);
		}
	}

	internal sealed record AsyncReceiverProcessor<TResult>
	(
		Signaler<TSignal> Signaler,
		Func<TSignal, Task<TResult>> Receiver,
		Module? Module = null
	) : Processor(Signaler, Module)
	{
		public override async IAsyncEnumerable<TResponse> OnNext<TResponse>(
			TSignal signal,
			Func<TSignal, IAsyncEnumerable<TResponse>> next
		)
		{
			if(await Receiver(signal) is TResponse response)
				yield return response;
			await foreach(var value in next(signal))
				yield return value;
		}
	}

	internal sealed record AsyncReceiverProcessor
	(
		Signaler<TSignal> Signaler,
		Func<TSignal, Task> Receiver,
		Module? Module = null
	) : Processor(Signaler, Module)
	{
		public override async IAsyncEnumerable<TResponse> OnNext<TResponse>(
			TSignal signal,
			Func<TSignal, IAsyncEnumerable<TResponse>> next
		)
		{
			await Receiver(signal);
			await foreach(var value in next(signal))
				yield return value;
		}
	}

	internal sealed record AsyncInterceptorProcessor<TResult>
	(
		Signaler<TSignal> Signaler,
		Func<SignalContext<TSignal, TResult>, IAsyncEnumerable<TResult>> Interceptor,
		Module? Module = null
	) : Processor(Signaler, Module)
	{
		public override IAsyncEnumerable<TResponse> OnNext<TResponse>(
			TSignal signal,
			Func<TSignal, IAsyncEnumerable<TResponse>> next
		) =>
		(
			Interceptor(new SignalContext<TSignal, TResult>(
				signal,
				next is Func<TSignal, IAsyncEnumerable<TResult>> nextResult
					? nextResult
					: s => next(s).Cast<TResponse, TResult>()
			)) switch
			{
				IAsyncEnumerable<TResponse> results => results,
				IAsyncEnumerable<TResult> results => results.Cast<TResult, TResponse>(),
				_ => AsyncEnumerable.Empty<TResponse>()
			}
		);
	}
}
