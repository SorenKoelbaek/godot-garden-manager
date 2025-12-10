#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GardenManager.Api;
using GardenManager.Auth;
using GardenManager.Models;
using Godot;

namespace GardenManager.UI
{
    public partial class SplashScreen : Control
    {
        private LineEdit _usernameInput;
        private LineEdit _passwordInput;
        private LineEdit _gameKeyInput;
        private Button _loginButton;
        private Label _errorLabel;
        private Label _statusLabel;
        private VBoxContainer _gardensContainer;
        private GardenManager.Auth.CredentialManager _credentialManager;
        private TokenManager _tokenManager;
        private ApiClient _apiClient;
        private AuthService _authService;
        private GardenService _gardenService;

        public override void _Ready()
        {
            GD.Print("SplashScreen: _Ready() called");
            
            // Get autoload singletons
            _tokenManager = GetNode<TokenManager>("/root/TokenManager");
            _apiClient = GetNode<ApiClient>("/root/ApiClient");
            
            GD.Print($"SplashScreen: TokenManager found: {_tokenManager != null}");
            GD.Print($"SplashScreen: ApiClient found: {_apiClient != null}");

            // Initialize services
            _credentialManager = new CredentialManager();
            _authService = new AuthService(_apiClient, _tokenManager, _credentialManager);
            _gardenService = new GardenService(_apiClient);
            GD.Print("SplashScreen: Services initialized");

            // Get UI nodes
            _usernameInput = GetNode<LineEdit>("VBoxContainer/UsernameInput");
            _passwordInput = GetNode<LineEdit>("VBoxContainer/PasswordInput");
            _gameKeyInput = GetNode<LineEdit>("VBoxContainer/GameKeyInput");
            _loginButton = GetNode<Button>("VBoxContainer/LoginButton");
            _errorLabel = GetNode<Label>("VBoxContainer/ErrorLabel");
            _statusLabel = GetNode<Label>("VBoxContainer/StatusLabel");
            _gardensContainer = GetNode<VBoxContainer>("VBoxContainer/GardensContainer");
            
            GD.Print($"SplashScreen: UI nodes found - Username: {_usernameInput != null}, Password: {_passwordInput != null}, GameKey: {_gameKeyInput != null}, Button: {_loginButton != null}");

            // Connect button
            _loginButton.Pressed += OnLoginButtonPressed;
            GD.Print("SplashScreen: Login button connected");

            // Hide gardens container initially
            _gardensContainer.Visible = false;

            // Load saved credentials
            var credentials = LoadSavedCredentials();

            // Start auto-login after tree is ready
            _ = AttemptAutoLoginAsync(credentials);
        }

        private async Task AttemptAutoLoginAsync(Credentials? credentials)
        {
            // Wait one frame to ensure the tree is fully ready
            await GetTree().ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

            // Check if already authenticated
            if (_tokenManager != null && _tokenManager.IsAuthenticated)
            {
                GD.Print("SplashScreen: Already authenticated, loading gardens");
                await LoadGardensAsync();
            }
            // If credentials are saved, try to auto-login
            else if (credentials != null)
            {
                GD.Print("SplashScreen: Saved credentials found, attempting auto-login");
                _statusLabel.Text = "Logging in...";
                _statusLabel.Visible = true;
                await AutoLoginAsync(credentials);
            }
        }

        private Credentials? LoadSavedCredentials()
        {
            var credentials = _credentialManager.LoadCredentials();
            if (credentials != null)
            {
                _usernameInput.Text = credentials.Username;
                _passwordInput.Text = credentials.Password;
                _gameKeyInput.Text = credentials.GameKey;
                return credentials;
            }
            return null;
        }

        private async Task AutoLoginAsync(Credentials credentials)
        {
            GD.Print($"SplashScreen: Auto-login with username: {credentials.Username}");
            
            var success = await _authService.LoginAsync(credentials.Username, credentials.Password, credentials.GameKey);
            GD.Print($"SplashScreen: Auto-login result: {success}");

            if (success)
            {
                GD.Print("SplashScreen: Auto-login successful!");
                _statusLabel.Text = "Login successful! Loading gardens...";
                await LoadGardensAsync();
            }
            else
            {
                GD.Print("SplashScreen: Auto-login failed");
                _statusLabel.Visible = false;
                _errorLabel.Text = "Auto-login failed. Please check your credentials.";
                _errorLabel.Visible = true;
                _loginButton.Disabled = false;
            }
        }

        private async void OnLoginButtonPressed()
        {
            GD.Print("SplashScreen: Login button pressed!");
            
            var username = _usernameInput.Text;
            var password = _passwordInput.Text;
            var gameKey = _gameKeyInput.Text;

            GD.Print($"SplashScreen: Username: {username}, Password length: {password.Length}, GameKey length: {gameKey.Length}");

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(gameKey))
            {
                GD.Print("SplashScreen: Validation failed - empty fields");
                _errorLabel.Text = "Please fill in all fields";
                _errorLabel.Visible = true;
                return;
            }

            _errorLabel.Visible = false;
            _statusLabel.Text = "Logging in...";
            _statusLabel.Visible = true;
            _loginButton.Disabled = true;

            GD.Print("SplashScreen: Calling LoginAsync...");
            var success = await _authService.LoginAsync(username, password, gameKey);
            GD.Print($"SplashScreen: LoginAsync returned: {success}");

            if (success)
            {
                GD.Print("SplashScreen: Login successful!");
                _statusLabel.Text = "Login successful! Loading gardens...";
                await LoadGardensAsync();
            }
            else
            {
                GD.Print("SplashScreen: Login failed");
                _errorLabel.Text = "Login failed. Please check your credentials.";
                _errorLabel.Visible = true;
                _statusLabel.Visible = false;
                _loginButton.Disabled = false;
            }
        }

        private async Task LoadGardensAsync()
        {
            try
            {
                var user = await _gardenService.GetCurrentUserAsync();
                if (user != null)
                {
                    _statusLabel.Text = $"Welcome, {user.Username}!";
                }

                List<GardenManager.Models.Garden> gardens = await _gardenService.GetGardensAsync();

                // Hide login form
                _usernameInput.Visible = false;
                _passwordInput.Visible = false;
                _gameKeyInput.Visible = false;
                _loginButton.Visible = false;

                // Show gardens for user to select
                _gardensContainer.Visible = true;
                DisplayGardens(gardens);
            }
            catch (System.Exception ex)
            {
                GD.PrintErr($"Error loading gardens: {ex.Message}");
                _errorLabel.Text = $"Error loading gardens: {ex.Message}";
                _errorLabel.Visible = true;
            }
        }

        private void NavigateToWorld(string gardenUuid)
        {
            GD.Print($"SplashScreen: Navigating to 3D world with garden: {gardenUuid}");
            // Store garden UUID in GameManager
            var gameManager = GetNode<GameManager>("/root/GameManager");
            gameManager.SetCurrentGardenUuid(gardenUuid);
            
            // Change to world scene
            GetTree().ChangeSceneToFile("res://scenes/world/main_world.tscn");
            
            // Main menu will be available as autoload after scene change
        }

        private void DisplayGardens(List<GardenManager.Models.Garden> gardens)
        {
            // Clear existing garden labels
            foreach (Node child in _gardensContainer.GetChildren())
            {
                child.QueueFree();
            }

            if (gardens.Count == 0)
            {
                var noGardensLabel = new Label();
                noGardensLabel.Text = "No gardens found.";
                _gardensContainer.AddChild(noGardensLabel);
                return;
            }

            var titleLabel = new Label();
            titleLabel.Text = "Your Gardens (click to enter):";
            titleLabel.AddThemeFontSizeOverride("font_size", 18);
            _gardensContainer.AddChild(titleLabel);

            foreach (var garden in gardens)
            {
                var gardenButton = new Button();
                gardenButton.Text = $"{garden.Name} ({garden.Width}x{garden.Depth} {garden.Unit})";
                gardenButton.Pressed += () => OnGardenSelected(garden);
                _gardensContainer.AddChild(gardenButton);
            }
        }

        private void OnGardenSelected(GardenManager.Models.Garden garden)
        {
            GD.Print($"SplashScreen: Garden selected: {garden.Name} ({garden.GardenUuid})");
            NavigateToWorld(garden.GardenUuid);
        }
    }
}

