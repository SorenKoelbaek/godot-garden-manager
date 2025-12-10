#nullable enable
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;

public partial class ApiClient : Node
{
	private const string BaseUrl = "https://api.agritur.dk";
	private HttpRequest _httpRequest;
	private TokenManager? _tokenManager;
	private TaskCompletionSource<string>? _currentRequest;

	public override void _Ready()
	{
		GD.Print("ApiClient: _Ready() called");
		_httpRequest = new HttpRequest();
		AddChild(_httpRequest);
		_httpRequest.RequestCompleted += OnRequestCompleted;
		_tokenManager = GetNode<TokenManager>("/root/TokenManager");
		GD.Print($"ApiClient: TokenManager found: {_tokenManager != null}");
	}

	public async Task<T?> GetAsync<T>(string endpoint, bool requiresAuth = true)
	{
		return await RequestAsync<T>(endpoint, null, HttpClient.Method.Get, requiresAuth);
	}

	public async Task<T?> PostAsync<T>(string endpoint, object? body = null, bool requiresAuth = true, Dictionary<string, string>? customHeaders = null)
	{
		return await RequestAsync<T>(endpoint, body, HttpClient.Method.Post, requiresAuth, customHeaders);
	}

	private async Task<T?> RequestAsync<T>(string endpoint, object? body, HttpClient.Method method, bool requiresAuth, Dictionary<string, string>? customHeaders = null)
	{
		var url = $"{BaseUrl}{endpoint}";
		var headers = new List<string> { "Content-Type: application/json" };

		if (requiresAuth && _tokenManager != null && _tokenManager.IsAuthenticated)
		{
			headers.Add($"Authorization: Bearer {_tokenManager.AccessToken}");
		}

		if (customHeaders != null)
		{
			foreach (var header in customHeaders)
			{
				headers.Add($"{header.Key}: {header.Value}");
			}
		}

		string requestData = "";
		if (body != null)
		{
			requestData = JsonSerializer.Serialize(body);
		}

		_currentRequest = new TaskCompletionSource<string>();
		
		var error = _httpRequest.Request(url, headers.ToArray(), method, requestData);
		if (error != Error.Ok)
		{
			GD.PrintErr($"HTTP Request error: {error}");
			_currentRequest = null;
			return default(T);
		}

		var responseBody = await _currentRequest.Task;
		_currentRequest = null;

		if (string.IsNullOrEmpty(responseBody))
		{
			return default(T);
		}

		try
		{
			var options = new JsonSerializerOptions
			{
				PropertyNameCaseInsensitive = true
			};
			return JsonSerializer.Deserialize<T>(responseBody, options);
		}
		catch (JsonException ex)
		{
			GD.PrintErr($"JSON deserialization error: {ex.Message}");
			return default(T);
		}
	}

	private void OnRequestCompleted(long result, long responseCode, string[] responseHeaders, byte[] bodyBytes)
	{
		if (_currentRequest == null)
		{
			return;
		}

		var httpResult = (HttpRequest.Result)result;

		if (httpResult != HttpRequest.Result.Success)
		{
			GD.PrintErr($"HTTP Request failed: {httpResult}");
			_currentRequest.SetResult("");
			return;
		}

		if (responseCode != 200 && responseCode != 201)
		{
			GD.PrintErr($"HTTP Error: {responseCode}");
			var errorBody = Encoding.UTF8.GetString(bodyBytes);
			GD.PrintErr($"Error body: {errorBody}");
			_currentRequest.SetResult("");
			return;
		}

		var jsonString = Encoding.UTF8.GetString(bodyBytes);
		_currentRequest.SetResult(jsonString);
	}
}
