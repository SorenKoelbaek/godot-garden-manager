#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using GardenManager.Auth;
using GardenManager.Models;
using Godot;

namespace GardenManager.Api
{
    public class AuthService
    {
        private readonly ApiClient _apiClient;
        private readonly TokenManager _tokenManager;
        private readonly GardenManager.Auth.CredentialManager _credentialManager;

        public AuthService(ApiClient apiClient, TokenManager tokenManager, CredentialManager credentialManager)
        {
            _apiClient = apiClient;
            _tokenManager = tokenManager;
            _credentialManager = credentialManager;
        }

        public async Task<bool> LoginAsync(string username, string password, string gameKey)
        {
            GD.Print("AuthService: LoginAsync called");
            var url = "/token";

            // Login endpoint expects form data
            var formData = $"username={Uri.EscapeDataString(username)}&password={Uri.EscapeDataString(password)}&grant_type=password";
            GD.Print($"AuthService: Form data length: {formData.Length}");

            var customHeaders = new Dictionary<string, string>
            {
                { "Content-Type", "application/x-www-form-urlencoded" },
                { "X-Game-Key", gameKey }
            };

            GD.Print("AuthService: Calling PostFormDataAsync...");
            // We need to make a raw POST request with form data
            var tokenResponse = await PostFormDataAsync<TokenResponse>(url, formData, customHeaders);
            GD.Print($"AuthService: PostFormDataAsync returned: {tokenResponse != null}");

            if (tokenResponse != null && !string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                GD.Print("AuthService: Token received, setting tokens");
                _tokenManager.SetTokens(tokenResponse.AccessToken, tokenResponse.RefreshToken);

                // Save credentials
                var credentials = new Credentials
                {
                    Username = username,
                    Password = password,
                    GameKey = gameKey
                };
                _credentialManager.SaveCredentials(credentials);
                GD.Print("AuthService: Credentials saved");

                return true;
            }

            GD.Print("AuthService: Login failed - no token received");
            return false;
        }

        public async Task<bool> RefreshTokenAsync()
        {
            if (string.IsNullOrEmpty(_tokenManager.RefreshToken))
            {
                return false;
            }

            if (_tokenManager.IsRefreshing())
            {
                // Already refreshing, wait a bit
                await Task.Delay(100);
                return _tokenManager.IsAuthenticated;
            }

            _tokenManager.SetRefreshing(true);

            try
            {
                var refreshRequest = new
                {
                    refresh_token = _tokenManager.RefreshToken
                };

                var tokenResponse = await _apiClient.PostAsync<TokenResponse>("/refresh-token", refreshRequest, false);

                if (tokenResponse != null && !string.IsNullOrEmpty(tokenResponse.AccessToken))
                {
                    _tokenManager.SetTokens(tokenResponse.AccessToken, tokenResponse.RefreshToken);
                    _tokenManager.SetRefreshing(false);
                    return true;
                }
            }
            catch (System.Exception ex)
            {
                GD.PrintErr($"Token refresh error: {ex.Message}");
            }
            finally
            {
                _tokenManager.SetRefreshing(false);
            }

            // Refresh failed, clear tokens
            _tokenManager.ClearTokens();
            return false;
        }

        private async Task<T?> PostFormDataAsync<T>(string endpoint, string formData, Dictionary<string, string>? customHeaders = null)
        {
            GD.Print($"AuthService: PostFormDataAsync - endpoint: {endpoint}");
            var url = $"https://api.agritur.dk{endpoint}";
            GD.Print($"AuthService: Full URL: {url}");
            
            var headers = new List<string>();

            if (customHeaders != null)
            {
                foreach (var header in customHeaders)
                {
                    headers.Add($"{header.Key}: {header.Value}");
                    GD.Print($"AuthService: Header: {header.Key}: {header.Value}");
                }
            }

            var httpRequest = new HttpRequest();
            var parent = _apiClient.GetParent() ?? _apiClient.GetTree().Root;
            parent.AddChild(httpRequest);
            GD.Print("AuthService: HttpRequest created and added to scene");
            
            // Wait one frame to ensure the node is in the tree
            await parent.GetTree().ToSignal(parent.GetTree(), SceneTree.SignalName.ProcessFrame);
            GD.Print("AuthService: HttpRequest ready");

            var tcs = new TaskCompletionSource<T?>();

            void OnCompleted(long result, long responseCode, string[] responseHeaders, byte[] bodyBytes)
            {
                GD.Print($"AuthService: Request completed - result: {result}, responseCode: {responseCode}");
                httpRequest.RequestCompleted -= OnCompleted;
                httpRequest.QueueFree();

                var httpResult = (HttpRequest.Result)result;

                if (httpResult != HttpRequest.Result.Success)
                {
                    GD.PrintErr($"HTTP Request failed: {httpResult}");
                    tcs.SetResult(default(T));
                    return;
                }

                if (responseCode != 200 && responseCode != 201)
                {
                    GD.PrintErr($"HTTP Error: {responseCode}");
                    var errorBody = Encoding.UTF8.GetString(bodyBytes);
                    GD.PrintErr($"Error body: {errorBody}");
                    tcs.SetResult(default(T));
                    return;
                }

                try
                {
                    var jsonString = Encoding.UTF8.GetString(bodyBytes);
                    GD.Print($"AuthService: Response body length: {jsonString.Length}");
                    if (string.IsNullOrEmpty(jsonString))
                    {
                        GD.Print("AuthService: Empty response body");
                        tcs.SetResult(default(T));
                        return;
                    }

                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    var data = JsonSerializer.Deserialize<T>(jsonString, options);
                    GD.Print($"AuthService: Deserialized data: {data != null}");
                    tcs.SetResult(data);
                }
                catch (JsonException ex)
                {
                    GD.PrintErr($"JSON deserialization error: {ex.Message}");
                    GD.PrintErr($"Stack trace: {ex.StackTrace}");
                    tcs.SetResult(default(T));
                }
            }

            httpRequest.RequestCompleted += OnCompleted;
            
            // Wait one more frame to ensure the node is fully in the tree
            await parent.GetTree().ToSignal(parent.GetTree(), SceneTree.SignalName.ProcessFrame);
            
            GD.Print("AuthService: Sending request...");
            var error = httpRequest.Request(url, headers.ToArray(), HttpClient.Method.Post, formData);

            if (error != Error.Ok)
            {
                httpRequest.QueueFree();
                GD.PrintErr($"HTTP Request error: {error}");
                return default(T);
            }

            GD.Print("AuthService: Request sent, waiting for response...");
            return await tcs.Task;
        }
    }
}

