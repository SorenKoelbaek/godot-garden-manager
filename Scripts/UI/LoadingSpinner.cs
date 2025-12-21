using Godot;
using Serilog;

public partial class LoadingSpinner : Control
{
    private Label _loadingLabel;
    private ProgressBar _progressBar;
    private Label _progressLabel;
    
    public override void _Ready()
    {
        Log.Debug("LoadingSpinner: _Ready() called");
        
        // Disable input processing to prevent mouse movement during loading
        SetProcessInput(false);
        MouseFilter = MouseFilterEnum.Ignore;
        
        // Set highest process priority to ensure updates even during heavy loading
        ProcessMode = ProcessModeEnum.Always;
        ProcessPriority = int.MaxValue; // Highest priority
        
        // Get UI nodes
        _loadingLabel = GetNodeOrNull<Label>("CenterContainer/ProgressBarContainer/LoadingLabel");
        if (_loadingLabel == null)
        {
            Log.Error("LoadingSpinner: LoadingLabel node not found!");
        }
        
        _progressBar = GetNodeOrNull<ProgressBar>("CenterContainer/ProgressBarContainer/ProgressBar");
        if (_progressBar == null)
        {
            Log.Error("LoadingSpinner: ProgressBar node not found!");
        }
        else
        {
            _progressBar.MinValue = 0.0;
            _progressBar.MaxValue = 100.0;
            _progressBar.Value = 0.0;
        }
        
        _progressLabel = GetNodeOrNull<Label>("CenterContainer/ProgressBarContainer/ProgressLabel");
        if (_progressLabel == null)
        {
            Log.Error("LoadingSpinner: ProgressLabel node not found!");
        }
        
        // Start visible
        Visible = true;
    }
    
    public override void _Input(InputEvent @event)
    {
        // Block all input while loading screen is visible
        if (Visible)
        {
            GetViewport().SetInputAsHandled();
        }
    }
    
    public void SetProgress(float progress)
    {
        // Clamp progress between 0 and 100
        progress = Mathf.Clamp(progress, 0.0f, 100.0f);
        
        if (_progressBar != null)
        {
            _progressBar.Value = progress;
        }
        
        if (_progressLabel != null)
        {
            _progressLabel.Text = $"{progress:F1}%";
        }
        
        Log.Debug("LoadingSpinner: Progress updated to {Progress:F1}%", progress);
    }
    
    public void SetStatus(string status)
    {
        if (_loadingLabel != null)
        {
            _loadingLabel.Text = status;
        }
    }
    
    public void ShowSpinner()
    {
        Visible = true;
        SetProcessInput(false);
        MouseFilter = MouseFilterEnum.Ignore;
        ProcessMode = ProcessModeEnum.Always;
        SetProgress(0.0f);
        Log.Debug("LoadingSpinner: Loading spinner shown");
    }
    
    public void HideSpinner()
    {
        Visible = false;
        SetProcessInput(true);
        MouseFilter = MouseFilterEnum.Pass;
        Log.Debug("LoadingSpinner: Loading spinner hidden");
    }
}

