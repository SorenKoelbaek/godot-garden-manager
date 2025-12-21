using Godot;
using Serilog;

public partial class MainMenu : Control
{
    private Button _returnButton;
    private Button _changeGardensButton;
    private Button _settingsButton;
    private Button _logoutButton;
    private Button _exitButton;

    public override void _Ready()
    {
        Log.Debug("MainMenu: _Ready() called");
        
        // Get UI nodes
        _returnButton = GetNode<Button>("VBoxContainer/ReturnButton");
        _changeGardensButton = GetNode<Button>("VBoxContainer/ChangeGardensButton");
        _settingsButton = GetNode<Button>("VBoxContainer/SettingsButton");
        _logoutButton = GetNode<Button>("VBoxContainer/LogoutButton");
        _exitButton = GetNode<Button>("VBoxContainer/ExitButton");
        
        // Connect buttons
        _returnButton.Pressed += OnReturnPressed;
        _changeGardensButton.Pressed += OnChangeGardensPressed;
        _settingsButton.Pressed += OnSettingsPressed;
        _logoutButton.Pressed += OnLogoutPressed;
        _exitButton.Pressed += OnExitPressed;
        
        // Style exit button as red
        var styleBox = new StyleBoxFlat();
        styleBox.BgColor = new Color(0.8f, 0.2f, 0.2f);
        _exitButton.AddThemeStyleboxOverride("normal", styleBox);
        
        var hoverStyleBox = new StyleBoxFlat();
        hoverStyleBox.BgColor = new Color(0.9f, 0.3f, 0.3f);
        _exitButton.AddThemeStyleboxOverride("hover", hoverStyleBox);
        
        Log.Debug("MainMenu: Buttons connected");
    }

    private void OnReturnPressed()
    {
        Log.Debug("MainMenu: Return pressed - closing menu");
        Hide();
        Input.MouseMode = Input.MouseModeEnum.Captured;
        
        // Restore time-based lighting
        var mainWorld = GetTree().CurrentScene;
        var worldManager = mainWorld?.GetNodeOrNull<WorldManager>("MainWorld");
        if (worldManager != null)
        {
            worldManager.RestoreTimeBasedLighting();
        }
    }

    private void OnChangeGardensPressed()
    {
        Log.Information("MainMenu: Change gardens pressed - navigating to splash screen");
        // Clear current garden selection
        var gameManager = GetNode<GameManager>("/root/GameManager");
        gameManager.SetCurrentGardenUuid("");
        GetTree().ChangeSceneToFile("res://scenes/ui/splash_screen.tscn");
    }

    private void OnSettingsPressed()
    {
        Log.Debug("MainMenu: Settings pressed - opening settings");
        var settingsScene = GD.Load<PackedScene>("res://scenes/ui/menus/settings_menu.tscn");
        if (settingsScene != null)
        {
            var settingsInstance = settingsScene.Instantiate<Control>();
            GetTree().Root.AddChild(settingsInstance);
            Hide();
        }
        else
        {
            Log.Error("MainMenu: Failed to load settings scene");
        }
    }

    private void OnLogoutPressed()
    {
        Log.Information("MainMenu: Logout pressed");
        var tokenManager = GetNode<TokenManager>("/root/TokenManager");
        var credentialManager = new GardenManager.Auth.CredentialManager();
        
        tokenManager.ClearTokens();
        credentialManager.ClearCredentials();
        
        Log.Information("MainMenu: Logged out - navigating to splash screen");
        GetTree().ChangeSceneToFile("res://scenes/ui/splash_screen.tscn");
    }

    private void OnExitPressed()
    {
        Log.Information("MainMenu: Exit pressed - quitting game");
        GetTree().Quit();
    }

    public void ShowMenu()
    {
        Visible = true;
        Input.MouseMode = Input.MouseModeEnum.Visible;
        
        // Restore menu lighting
        var mainWorld = GetTree().CurrentScene;
        var worldManager = mainWorld?.GetNodeOrNull<WorldManager>("MainWorld");
        if (worldManager != null)
        {
            worldManager.RestoreMenuLighting();
        }
    }
}

