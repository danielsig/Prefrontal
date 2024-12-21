namespace Prefrontal.Modules.LLM;

// TODO: Make this class work by expecting an URL to an OpenAI compatible API endpoint, and a token to authenticate with.
/// <summary>
/// Work in progress.
/// </summary>
public class RemoteLLMModule
(
	HttpClient httpClient
) : Module, IDialogContinuator
{
	private readonly HttpClient _httpClient = httpClient;
	public string? Endpoint { get; private set; }
	private string? _apiKey;

	public void Configure(string endpoint, string apiKey)
	{
		Endpoint = endpoint;
		_apiKey = apiKey;

		ReceiveSignals((Dialog dialog) =>
		{
			// TODO: Async signals need to work
		});
	}

	protected override async Task InitializeAsync()
	{
		if(string.IsNullOrWhiteSpace(Endpoint))
			throw new InvalidOperationException("Endpoint for the HTTP Language Model is not set.");
		if(string.IsNullOrWhiteSpace(_apiKey))
			throw new InvalidOperationException("The API key for the HTTP Language Model is not set.");
		
		_httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + _apiKey);
		_httpClient.DefaultRequestHeaders.Add("Content-Type", "application/json");
	}

	public Task<DialogContinuation> ContinueAsync(Dialog context)
	{
		throw new NotImplementedException();
	}

	public IAsyncEnumerable<DialogContinuation> Continue(Dialog dialog)
	{
		throw new NotImplementedException();
	}
}
