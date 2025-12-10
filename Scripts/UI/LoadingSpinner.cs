using Godot;

public partial class LoadingSpinner : Control
{
    private Label _loadingLabel;
    
    public override void _Ready()
    {
        GD.Print("LoadingSpinner: _Ready() called");
        
        // Get loading label
        _loadingLabel = GetNodeOrNull<Label>("LoadingLabel");
        if (_loadingLabel == null)
        {
            GD.PrintErr("LoadingSpinner: LoadingLabel node not found!");
        }
        
        // Start visible
        Visible = true;
    }
    
    public void ShowSpinner()
    {
        Visible = true;
        GD.Print("LoadingSpinner: Loading text shown");
    }
    
    public void HideSpinner()
    {
        Visible = false;
        GD.Print($"LoadingSpinner: Loading text hidden");
    }
}

