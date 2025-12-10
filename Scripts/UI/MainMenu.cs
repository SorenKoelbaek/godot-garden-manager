using Godot;

public partial class MainMenu : Control
{
    private Button _returnButton;
    private Button _changeGardensButton;
    private Button _settingsButton;
    private Button _logoutButton;
    private Button _exitButton;

    public override void _Ready()
    {
        GD.Print("MainMenu: _Ready() called");
        
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
        
        GD.Print("MainMenu: Buttons connected");
    }

    private void OnReturnPressed()
    {
        GD.Print("MainMenu: Return pressed - closing menu");
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
        GD.Print("MainMenu: Change gardens pressed - navigating to splash screen");
        // Clear current garden selection
        var gameManager = GetNode<GameManager>("/root/GameManager");
        gameManager.SetCurrentGardenUuid("");
        GetTree().ChangeSceneToFile("res://scenes/ui/splash_screen.tscn");
    }

    private void OnSettingsPressed()
    {
        GD.Print("MainMenu: Settings pressed - opening settings");
        var settingsScene = GD.Load<PackedScene>("res://scenes/ui/menus/settings_menu.tscn");
        if (settingsScene != null)
        {
            var settingsInstance = settingsScene.Instantiate<Control>();
            GetTree().Root.AddChild(settingsInstance);
            Hide();
        }
        else
        {
            GD.PrintErr("MainMenu: Failed to load settings scene");
        }
    }

    private void OnLogoutPressed()
    {
        GD.Print("MainMenu: Logout pressed");
        var tokenManager = GetNode<TokenManager>("/root/TokenManager");
        var credentialManager = new GardenManager.Auth.CredentialManager();
        
        tokenManager.ClearTokens();
        credentialManager.ClearCredentials();
        
        GD.Print("MainMenu: Logged out - navigating to splash screen");
        GetTree().ChangeSceneToFile("res://scenes/ui/splash_screen.tscn");
    }

    private void OnExitPressed()
    {
        GD.Print("MainMenu: Exit pressed - quitting game");
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

