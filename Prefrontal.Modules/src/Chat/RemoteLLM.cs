using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Prefrontal.Common.Extensions;

namespace Prefrontal.Modules.Chat;

public class RemoteLLM<TMessage> : LLM<TMessage>
{
	private readonly HttpClient _httpClient;
	private readonly JsonSerializerOptions _jsonSerializerOptions;

	public RemoteLLM(
		string openAICompatibleEndpoint,
		string apiKey,
		JsonSerializerOptions? jsonSerializerOptions = null
	)
	{
		if(string.IsNullOrWhiteSpace(openAICompatibleEndpoint))
			throw new InvalidOperationException("The OpenAI compatible endpoint must be set.");
		if(string.IsNullOrWhiteSpace(apiKey))
			throw new InvalidOperationException("The API key must be set.");

		if(!openAICompatibleEndpoint.EndsWith('/'))
			openAICompatibleEndpoint += '/';

		_httpClient = new();
		_httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + apiKey);
		//_httpClient.DefaultRequestHeaders.Add("Content-Type", "application/json");
		_httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
		_httpClient.BaseAddress = new(openAICompatibleEndpoint);

		_jsonSerializerOptions
			= jsonSerializerOptions
			?? DEFAULT_JSON_SERIALIZER_OPTIONS;
	}


	#region assigned right before serialization
	#pragma warning disable IDE0052, CS0414
	[JsonInclude, JsonPropertyName("stream")] private bool _stream = false;
	[JsonInclude, JsonPropertyName("n")] private int _n = 1;
	#pragma warning restore IDE0052, CS0414
	#endregion



	public override async Task<List<ModelInfo>> GetAvailableModelsAsync(CancellationToken cancellationToken = default)
	{
		var response = await _httpClient.GetAsync("models", cancellationToken);
		if(!response.IsSuccessStatusCode)
			return [];
		return await response.Content.ReadFromJsonAsync<List<ModelInfo>>(_jsonSerializerOptions, cancellationToken)
			?? [];
	}

	public override async IAsyncEnumerable<CompletionChunk> ContinueAsync(int n, [EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		_n = n;
		_stream = true;
		var response = _httpClient.Send(
			new HttpRequestMessage(
				HttpMethod.Post,
				"chat/completions"
			)
			{
				Content = new StringContent(
					JsonSerializer.Serialize(this, _jsonSerializerOptions),
					Encoding.UTF8,
					"application/json"
				)
			},
			HttpCompletionOption.ResponseHeadersRead,
			cancellationToken
		);
		if(!response.IsSuccessStatusCode)
			yield break;

		using var responseStream = new StreamReader(await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false));
		while(!responseStream.EndOfStream)
		{
			if(cancellationToken.IsCancellationRequested)
				yield break;

			var line = await responseStream.ReadLineAsync(cancellationToken);
			if(line is null or "")
				continue;
			if(line == "data: [DONE]")
				break;

			var chunk = JsonSerializer.Deserialize<CompletionChunk>(
				line.After("data: "),
				_jsonSerializerOptions
			);

			if(chunk is not null)
				yield return chunk;
		}
	}

	public override async Task<CompletionChunk> CompleteAsync(int n, CancellationToken cancellationToken = default)
	{
		_n = n;
		_stream = false;
		var response = await _httpClient.PostAsJsonAsync("chat/completions", this, _jsonSerializerOptions);
		if(!response.IsSuccessStatusCode)
			return CreateEmptyCompletionChunk(n);
		return await response.Content.ReadFromJsonAsync<CompletionChunk>(_jsonSerializerOptions, cancellationToken)
			?? CreateEmptyCompletionChunk(n);
	}

	public override async Task<List<EmbeddingObject>> GetEmbeddingsAsync(
		List<string> input,
		int? dimensions = null,
		string? user = null,
		CancellationToken cancellationToken = default
	)
	{
		var requestBody = new
		{
			Input = input,
			Dimensions = dimensions,
			User = user
		};
		var response = await _httpClient.PostAsJsonAsync("embeddings", requestBody, _jsonSerializerOptions);
		if(!response.IsSuccessStatusCode)
			return [];
		return await response.Content.ReadFromJsonAsync<List<EmbeddingObject>>(_jsonSerializerOptions, cancellationToken)
			?? [];
	}

	public static readonly JsonSerializerOptions DEFAULT_JSON_SERIALIZER_OPTIONS = new()
	{
		WriteIndented = true,
		IncludeFields = true,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
		Converters =
		{
			new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower)
		},
	};
}
