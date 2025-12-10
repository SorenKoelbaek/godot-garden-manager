using Godot;

public partial class GameHUD : Control
{
    private Label _timeLabel;
    private Label _monthLabel;
    private Label _yearLabel;
    private Button _slowDownButton;
    private Button _speedUpButton;
    private Button _prevMonthButton;
    private Button _nextMonthButton;
    private TimeManager _timeManager;
    private MainMenu _mainMenu;
    
    public override void _Ready()
    {
        GD.Print("GameHUD: _Ready() called");
        
        // Get TimeManager singleton
        _timeManager = GetNode<TimeManager>("/root/TimeManager");
        
        // Get UI nodes
        _timeLabel = GetNode<Label>("HBoxContainer/TimeContainer/TimeLabel");
        _monthLabel = GetNode<Label>("HBoxContainer/MonthContainer/MonthLabel");
        _yearLabel = GetNode<Label>("HBoxContainer/YearLabel");
        _slowDownButton = GetNode<Button>("HBoxContainer/TimeContainer/SlowDownButton");
        _speedUpButton = GetNode<Button>("HBoxContainer/TimeContainer/SpeedUpButton");
        _prevMonthButton = GetNode<Button>("HBoxContainer/MonthContainer/PrevMonthButton");
        _nextMonthButton = GetNode<Button>("HBoxContainer/MonthContainer/NextMonthButton");
        
        // Connect buttons
        _slowDownButton.Pressed += OnSlowDownPressed;
        _speedUpButton.Pressed += OnSpeedUpPressed;
        _prevMonthButton.Pressed += OnPrevMonthPressed;
        _nextMonthButton.Pressed += OnNextMonthPressed;
        
        // Subscribe to time changes
        _timeManager.TimeChanged += OnTimeChanged;
        _timeManager.DateChanged += OnDateChanged;
        
        // Initial update
        UpdateTimeDisplay();
        UpdateDateDisplay();
        
        // Find MainMenu to listen for visibility changes
        var mainWorld = GetTree().CurrentScene;
        _mainMenu = mainWorld?.GetNodeOrNull<MainMenu>("UICanvas/MainMenu");
        
        // Start hidden until loading is complete
        Visible = false;
        
        GD.Print("GameHUD: Initialized (hidden until loading complete)");
    }
    
    public void ShowHUD()
    {
        _loadingComplete = true;
        Visible = true;
        GD.Print("GameHUD: HUD shown");
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
    
}

