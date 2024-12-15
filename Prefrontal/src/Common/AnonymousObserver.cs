namespace Prefrontal.Reactive;

/// <summary>
/// Represents an observer that can receive notifications of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of the elements in the sequence.</typeparam>
internal sealed class AnonymousObserver<T>(
	Action<T> _onNext,
	Action<Exception> _onError,
	Action _onCompleted
) : IObserver<T>
{
	/// <inheritdoc />
	public void OnNext(T value) => _onNext(value);

	/// <inheritdoc />
	public void OnError(Exception error) => _onError(error);

	/// <inheritdoc />
	public void OnCompleted() => _onCompleted();
}
