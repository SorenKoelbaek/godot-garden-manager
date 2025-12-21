#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using GardenManager.Auth;
using GardenManager.Models;
using Godot;
using Serilog;

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
            Log.Debug("AuthService: LoginAsync called");
            var url = "/token";

            // Login endpoint expects form data
            var formData = $"username={Uri.EscapeDataString(username)}&password={Uri.EscapeDataString(password)}&grant_type=password";
            Log.Debug("AuthService: Form data length: {Length}", formData.Length);

            var customHeaders = new Dictionary<string, string>
            {
                { "Content-Type", "application/x-www-form-urlencoded" },
                { "X-Game-Key", gameKey }
            };

            Log.Debug("AuthService: Calling PostFormDataAsync...");
            // We need to make a raw POST request with form data
            var tokenResponse = await PostFormDataAsync<TokenResponse>(url, formData, customHeaders);
            Log.Debug("AuthService: PostFormDataAsync returned: {HasResponse}", tokenResponse != null);

            if (tokenResponse != null && !string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                Log.Debug("AuthService: Token received, setting tokens");
                _tokenManager.SetTokens(tokenResponse.AccessToken, tokenResponse.RefreshToken);

                // Save credentials
                var credentials = new Credentials
                {
                    Username = username,
                    Password = password,
                    GameKey = gameKey
                };
                _credentialManager.SaveCredentials(credentials);
                Log.Information("AuthService: Credentials saved");

                return true;
            }

            Log.Warning("AuthService: Login failed - no token received");
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
                Log.Error(ex, "AuthService: Token refresh error");
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
            Log.Debug("AuthService: PostFormDataAsync - endpoint: {Endpoint}", endpoint);
            var url = $"https://api.agritur.dk{endpoint}";
            Log.Debug("AuthService: Full URL: {Url}", url);
            
            var headers = new List<string>();

            if (customHeaders != null)
            {
                foreach (var header in customHeaders)
                {
                    headers.Add($"{header.Key}: {header.Value}");
                    // Security: Do not log header values (may contain sensitive data like X-Game-Key)
                    Log.Debug("AuthService: Adding header: {HeaderName}", header.Key);
                }
            }

            var httpRequest = new HttpRequest();
            var parent = _apiClient.GetParent() ?? _apiClient.GetTree().Root;
            parent.AddChild(httpRequest);
            Log.Debug("AuthService: HttpRequest created and added to scene");
            
            // Wait one frame to ensure the node is in the tree
            await parent.GetTree().ToSignal(parent.GetTree(), SceneTree.SignalName.ProcessFrame);
            Log.Debug("AuthService: HttpRequest ready");

            var tcs = new TaskCompletionSource<T?>();

            void OnCompleted(long result, long responseCode, string[] responseHeaders, byte[] bodyBytes)
            {
                Log.Debug("AuthService: Request completed - result: {Result}, responseCode: {ResponseCode}", result, responseCode);
                httpRequest.RequestCompleted -= OnCompleted;
                httpRequest.QueueFree();

                var httpResult = (HttpRequest.Result)result;

                if (httpResult != HttpRequest.Result.Success)
                {
                    Log.Error("AuthService: HTTP Request failed: {Result}", httpResult);
                    tcs.SetResult(default(T));
                    return;
                }

                if (responseCode != 200 && responseCode != 201)
                {
                    Log.Error("AuthService: HTTP Error: {ResponseCode}", responseCode);
                    var errorBody = Encoding.UTF8.GetString(bodyBytes);
                    Log.Error("AuthService: Error body: {ErrorBody}", errorBody);
                    tcs.SetResult(default(T));
                    return;
                }

                try
                {
                    var jsonString = Encoding.UTF8.GetString(bodyBytes);
                    Log.Debug("AuthService: Response body length: {Length}", jsonString.Length);
                    if (string.IsNullOrEmpty(jsonString))
                    {
                        Log.Debug("AuthService: Empty response body");
                        tcs.SetResult(default(T));
                        return;
                    }

                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    var data = JsonSerializer.Deserialize<T>(jsonString, options);
                    Log.Debug("AuthService: Deserialized data: {HasData}", data != null);
                    tcs.SetResult(data);
                }
                catch (JsonException ex)
                {
                    Log.Error(ex, "AuthService: JSON deserialization error");
                    tcs.SetResult(default(T));
                }
            }

            httpRequest.RequestCompleted += OnCompleted;
            
            // Wait one more frame to ensure the node is fully in the tree
            await parent.GetTree().ToSignal(parent.GetTree(), SceneTree.SignalName.ProcessFrame);
            
            Log.Debug("AuthService: Sending request...");
            var error = httpRequest.Request(url, headers.ToArray(), HttpClient.Method.Post, formData);

            if (error != Error.Ok)
            {
                httpRequest.QueueFree();
                Log.Error("AuthService: HTTP Request error: {Error}", error);
                return default(T);
            }

            Log.Debug("AuthService: Request sent, waiting for response...");
            return await tcs.Task;
        }
    }
}

