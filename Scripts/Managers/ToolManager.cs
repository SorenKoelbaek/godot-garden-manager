using Godot;
using Serilog;

public enum ToolType
{
    Hands,
    Mower
}

public partial class ToolManager : Node
{
    private ToolType _currentTool = ToolType.Hands;
    
    public ToolType CurrentTool
    {
        get => _currentTool;
        private set
        {
            if (_currentTool != value)
            {
                _currentTool = value;
                ToolChanged?.Invoke(value);
                Log.Debug("ToolManager: Tool changed to {Tool}", value);
            }
        }
    }
    
    public event System.Action<ToolType> ToolChanged;
    
    public override void _Ready()
    {
        Log.Information("ToolManager: Initialized");
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
            _ => "Unknown"
        };
    }
}

