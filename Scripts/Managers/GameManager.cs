using Godot;
using Serilog;

public partial class GameManager : Node
{
    private string _currentGardenUuid = string.Empty;

    public string CurrentGardenUuid => _currentGardenUuid;

    public override void _Ready()
    {
    }

    public void SetCurrentGardenUuid(string gardenUuid)
    {
        Log.Debug("GameManager: Setting current garden UUID: {GardenUuid}", gardenUuid);
        _currentGardenUuid = gardenUuid;
    }
}


