using GardenManager.Auth;
using GardenManager.Models;
using Godot;

public partial class SettingsMenu : Control
{
    private HSlider _mouseSensitivitySlider;
    private HSlider _playerSpeedSlider;
    private Label _mouseSensitivityLabel;
    private Label _playerSpeedLabel;
    private CheckBox _renderGrassCheckBox;
    private CheckBox _useAdvancedSkyCheckBox;
    private Button _saveButton;
    private Button _cancelButton;
    
    private CredentialManager _credentialManager;
    private GameSettings _settings;

    public override void _Ready()
    {
        GD.Print("SettingsMenu: _Ready() called");
        
        _credentialManager = new CredentialManager();
        
        // Get UI nodes
        _mouseSensitivitySlider = GetNode<HSlider>("VBoxContainer/MouseSensitivityContainer/HSlider");
        _playerSpeedSlider = GetNode<HSlider>("VBoxContainer/PlayerSpeedContainer/HSlider");
        _mouseSensitivityLabel = GetNode<Label>("VBoxContainer/MouseSensitivityContainer/Label");
        _playerSpeedLabel = GetNode<Label>("VBoxContainer/PlayerSpeedContainer/Label");
        _renderGrassCheckBox = GetNode<CheckBox>("VBoxContainer/RenderGrassContainer/RenderGrassCheckBox");
        _useAdvancedSkyCheckBox = GetNode<CheckBox>("VBoxContainer/UseAdvancedSkyContainer/UseAdvancedSkyCheckBox");
        _saveButton = GetNode<Button>("VBoxContainer/SaveButton");
        _cancelButton = GetNode<Button>("VBoxContainer/CancelButton");
        
        // Connect buttons
        _saveButton.Pressed += OnSavePressed;
        _cancelButton.Pressed += OnCancelPressed;
        
        // Connect sliders
        _mouseSensitivitySlider.ValueChanged += OnMouseSensitivityChanged;
        _playerSpeedSlider.ValueChanged += OnPlayerSpeedChanged;
        
        // Connect checkboxes
        _renderGrassCheckBox.Toggled += OnRenderGrassToggled;
        _useAdvancedSkyCheckBox.Toggled += OnUseAdvancedSkyToggled;
        
        // Load settings
        _settings = _credentialManager.LoadSettings();
        if (_settings == null)
        {
            _settings = new GameSettings
            {
                MouseSensitivity = 0.003f,
                PlayerSpeed = 5.0f,
                RenderGrass = true,
                UseAdvancedSky = false
            };
        }
        
        // Set slider values
        _mouseSensitivitySlider.MinValue = 0.001f;
        _mouseSensitivitySlider.MaxValue = 0.01f;
        _mouseSensitivitySlider.Step = 0.0001f;
        _mouseSensitivitySlider.Value = _settings.MouseSensitivity;
        
        _playerSpeedSlider.MinValue = 1.0f;
        _playerSpeedSlider.MaxValue = 20.0f;
        _playerSpeedSlider.Step = 0.1f;
        _playerSpeedSlider.Value = _settings.PlayerSpeed;
        
        // Set checkbox values
        _renderGrassCheckBox.ButtonPressed = _settings.RenderGrass;
        _useAdvancedSkyCheckBox.ButtonPressed = _settings.UseAdvancedSky;
        
        UpdateLabels();
        
        GD.Print("SettingsMenu: Initialized");
    }

    private void OnMouseSensitivityChanged(double value)
    {
        _settings.MouseSensitivity = (float)value;
        UpdateLabels();
    }

    private void OnPlayerSpeedChanged(double value)
    {
        _settings.PlayerSpeed = (float)value;
        UpdateLabels();
    }

    private void OnRenderGrassToggled(bool buttonPressed)
    {
        _settings.RenderGrass = buttonPressed;
        GD.Print($"SettingsMenu: Render grass toggled: {buttonPressed}");
    }

    private void OnUseAdvancedSkyToggled(bool buttonPressed)
    {
        _settings.UseAdvancedSky = buttonPressed;
        GD.Print($"SettingsMenu: Use advanced sky toggled: {buttonPressed}");
    }

    private void UpdateLabels()
    {
        _mouseSensitivityLabel.Text = $"Mouse Sensitivity: {_settings.MouseSensitivity:F4}";
        _playerSpeedLabel.Text = $"Player Speed: {_settings.PlayerSpeed:F1} m/s";
    }

    private void OnSavePressed()
    {
        GD.Print($"SettingsMenu: Saving settings - Sensitivity: {_settings.MouseSensitivity}, Speed: {_settings.PlayerSpeed}, RenderGrass: {_settings.RenderGrass}, UseAdvancedSky: {_settings.UseAdvancedSky}");
        _credentialManager.SaveSettings(_settings);
        
        // Apply to player if in world
        var mainWorld = GetTree().CurrentScene;
        if (mainWorld != null)
        {
            var player = mainWorld.GetNodeOrNull<Player>("Player");
            if (player != null)
            {
                player.MouseSensitivity = _settings.MouseSensitivity;
                player.Speed = _settings.PlayerSpeed;
                GD.Print("SettingsMenu: Applied settings to player");
            }
            
            // Apply grass setting to garden if in world
            var garden = mainWorld.GetNodeOrNull<Garden>("Garden");
            if (garden != null)
            {
                garden.SetGrassVisible(_settings.RenderGrass);
                GD.Print($"SettingsMenu: Applied grass setting to garden: {_settings.RenderGrass}");
            }
            
            // Apply advanced sky setting to world manager if in world
            var worldManager = mainWorld as WorldManager;
            if (worldManager != null)
            {
                worldManager.SetAdvancedSky(_settings.UseAdvancedSky);
                GD.Print($"SettingsMenu: Applied advanced sky setting: {_settings.UseAdvancedSky}");
            }
        }
        
        QueueFree();
    }

    private void OnCancelPressed()
    {
        GD.Print("SettingsMenu: Cancel pressed");
        QueueFree();
    }
}

