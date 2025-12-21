using Godot;
using Serilog;

public partial class GameHUD : Control
{
    private Label _timeLabel;
    private Label _monthLabel;
    private Label _yearLabel;
    private Button _slowDownButton;
    private Button _speedUpButton;
    private Button _prevMonthButton;
    private Button _nextMonthButton;
    private Label _toolLabel;
    private TimeManager _timeManager;
    private ToolManager _toolManager;
    private MainMenu _mainMenu;
    
    public override void _Ready()
    {
        Log.Debug("GameHUD: _Ready() called");
        
        // Get singletons
        _timeManager = GetNode<TimeManager>("/root/TimeManager");
        _toolManager = GetNode<ToolManager>("/root/ToolManager");
        
        // Get UI nodes
        _timeLabel = GetNode<Label>("HBoxContainer/TimeContainer/TimeLabel");
        _monthLabel = GetNode<Label>("HBoxContainer/MonthContainer/MonthLabel");
        _yearLabel = GetNode<Label>("HBoxContainer/YearLabel");
        _slowDownButton = GetNode<Button>("HBoxContainer/TimeContainer/SlowDownButton");
        _speedUpButton = GetNode<Button>("HBoxContainer/TimeContainer/SpeedUpButton");
        _prevMonthButton = GetNode<Button>("HBoxContainer/MonthContainer/PrevMonthButton");
        _nextMonthButton = GetNode<Button>("HBoxContainer/MonthContainer/NextMonthButton");
        _toolLabel = GetNode<Label>("HBoxContainer/ToolLabel");
        
        // Connect buttons
        _slowDownButton.Pressed += OnSlowDownPressed;
        _speedUpButton.Pressed += OnSpeedUpPressed;
        _prevMonthButton.Pressed += OnPrevMonthPressed;
        _nextMonthButton.Pressed += OnNextMonthPressed;
        
        // Subscribe to time changes
        _timeManager.TimeChanged += OnTimeChanged;
        _timeManager.DateChanged += OnDateChanged;
        
        // Subscribe to tool changes
        if (_toolManager != null)
        {
            _toolManager.ToolChanged += OnToolChanged;
        }
        
        // Initial update
        UpdateTimeDisplay();
        UpdateDateDisplay();
        UpdateToolDisplay();
        
        // Find MainMenu to listen for visibility changes
        var mainWorld = GetTree().CurrentScene;
        _mainMenu = mainWorld?.GetNodeOrNull<MainMenu>("UICanvas/MainMenu");
        
        // Start hidden until loading is complete
        Visible = false;
        
        Log.Debug("GameHUD: Initialized (hidden until loading complete)");
    }
    
    public void ShowHUD()
    {
        _loadingComplete = true;
        Visible = true;
        Log.Debug("GameHUD: HUD shown");
    }
    
    private bool _loadingComplete = false;
    
    public override void _Process(double delta)
    {
        // Only update HUD visibility based on menu state if loading is complete
        if (_loadingComplete && _mainMenu != null)
        {
            Visible = !_mainMenu.Visible;
        }
    }
    
    public override void _ExitTree()
    {
        if (_timeManager != null)
        {
            _timeManager.TimeChanged -= OnTimeChanged;
            _timeManager.DateChanged -= OnDateChanged;
        }
        
        if (_toolManager != null)
        {
            _toolManager.ToolChanged -= OnToolChanged;
        }
    }
    
    private void OnTimeChanged(float timeMinutes)
    {
        UpdateTimeDisplay();
    }
    
    private void OnDateChanged(int month, int year)
    {
        UpdateDateDisplay();
    }
    
    private void UpdateTimeDisplay()
    {
        if (_timeLabel != null && _timeManager != null)
        {
            _timeLabel.Text = _timeManager.GetTimeString();
        }
    }
    
    private void UpdateDateDisplay()
    {
        if (_monthLabel != null && _yearLabel != null && _timeManager != null)
        {
            _monthLabel.Text = _timeManager.GetMonthString();
            _yearLabel.Text = _timeManager.CurrentYear.ToString();
        }
    }
    
    private void OnSlowDownPressed()
    {
        _timeManager?.SlowDownTime();
    }
    
    private void OnSpeedUpPressed()
    {
        _timeManager?.SpeedUpTime();
    }
    
    private void OnPrevMonthPressed()
    {
        _timeManager?.PreviousMonth();
    }
    
    private void OnNextMonthPressed()
    {
        _timeManager?.NextMonth();
    }
    
    private void OnToolChanged(ToolType tool)
    {
        UpdateToolDisplay();
    }
    
    private void UpdateToolDisplay()
    {
        if (_toolLabel != null && _toolManager != null)
        {
            _toolLabel.Text = $"Tool: {_toolManager.GetToolName()}";
        }
    }
    
}

