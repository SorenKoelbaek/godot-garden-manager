using Godot;

public partial class GameManager : Node
{
    private string _currentGardenUuid = string.Empty;

    public string CurrentGardenUuid => _currentGardenUuid;

    public void SetCurrentGardenUuid(string gardenUuid)
    {
        GD.Print($"GameManager: Setting current garden UUID: {gardenUuid}");
        _currentGardenUuid = gardenUuid;
    }
}


