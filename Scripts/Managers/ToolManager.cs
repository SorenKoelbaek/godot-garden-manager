using Godot;
using Serilog;

public enum ToolType
{
    Hands,
    Mower,
    Hoe,
    Plant
}

public class ToolInfo
{
    public string IconPath { get; set; } = string.Empty;
    public float DiscDiameter { get; set; } = 0.1f; // Default 10cm
}

public partial class ToolManager : Node
{
    private ToolType _currentTool = ToolType.Hands;
    
    // Tool configuration: icon path and disc diameter for each tool
    private readonly System.Collections.Generic.Dictionary<ToolType, ToolInfo> _toolConfigs = new()
    {
        { ToolType.Hands, new ToolInfo { IconPath = "", DiscDiameter = 0.1f } }, // No icon, 10cm disc
        { ToolType.Mower, new ToolInfo { IconPath = "res://resources/ui/mower.png", DiscDiameter = 0.6f } }, // 80cm disc
        { ToolType.Hoe, new ToolInfo { IconPath = "res://resources/ui/hoe.png", DiscDiameter = 0.1f } }, // 10cm disc
        { ToolType.Plant, new ToolInfo { IconPath = "res://resources/ui/plant.png", DiscDiameter = 0.1f } } // 10cm disc
    };
    
    public ToolType CurrentTool
    {
        get => _currentTool;
        private set
        {
            if (_currentTool != value)
            {
                _currentTool = value;
                ToolChanged?.Invoke(value);
                
                // Emit detailed tool changed event with icon and size
                if (_toolConfigs.TryGetValue(value, out var toolInfo))
                {
                    ToolChangedDetailed?.Invoke(value, toolInfo.IconPath, toolInfo.DiscDiameter);
                }
                
                Log.Debug("ToolManager: Tool changed to {Tool}", value);
            }
        }
    }
    
    public event System.Action<ToolType> ToolChanged;
    public event System.Action<ToolType, string, float> ToolChangedDetailed; // ToolType, IconPath, DiscDiameter
    
    public override void _Ready()
    {
        Log.Information("ToolManager: Initialized");
        // Emit initial tool info
        if (_toolConfigs.TryGetValue(_currentTool, out var toolInfo))
        {
            ToolChangedDetailed?.Invoke(_currentTool, toolInfo.IconPath, toolInfo.DiscDiameter);
        }
    }
    
    /// <summary>
    /// Get tool info for a specific tool type
    /// </summary>
    public ToolInfo? GetToolInfo(ToolType toolType)
    {
        return _toolConfigs.TryGetValue(toolType, out var info) ? info : null;
    }
    
    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed)
        {
            if (keyEvent.Keycode == Key.Key1)
            {
                CurrentTool = ToolType.Hands;
            }
            else if (keyEvent.Keycode == Key.Key2)
            {
                CurrentTool = ToolType.Mower;
            }
        }
    }
    
    public string GetToolName()
    {
        return CurrentTool switch
        {
            ToolType.Hands => "Hands",
            ToolType.Mower => "Mower",
            ToolType.Hoe => "Hoe",
            ToolType.Plant => "Plant",
            _ => "Unknown"
        };
    }
}

