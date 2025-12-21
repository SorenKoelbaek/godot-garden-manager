using Godot;
using System;
using Serilog;

public partial class TimeManager : Node
{
    // Time progression: 1 second = 1 minute (base speed)
    private float _timeSpeed = 1.0f; // 1.0 = normal, 0.5 = half speed, 2.0 = double speed, etc.
    
    // Current time (in minutes since midnight)
    private float _currentTimeMinutes = 0.0f;
    
    // Current date
    private int _currentMonth = DateTime.Now.Month;
    private int _currentYear = DateTime.Now.Year;
    
    // Track if we've already advanced month today (to prevent multiple advances)
    private bool _monthAdvancedToday = false;
    
    // Events
    public event Action<float> TimeChanged; // Passes current time in minutes
    public event Action<int, int> DateChanged; // Passes month, year
    
    public float CurrentTimeMinutes => _currentTimeMinutes;
    public int CurrentMonth => _currentMonth;
    public int CurrentYear => _currentYear;
    public float TimeSpeed => _timeSpeed;
    
    // Time speed multipliers
    private readonly float[] _speedMultipliers = { 0.25f, 0.5f, 1.0f, 2.0f, 4.0f, 8.0f, 16.0f, 32.0f };
    private int _currentSpeedIndex = 2; // Start at normal speed (1.0)
    
    public override void _Ready()
    {
        Log.Information("TimeManager: Initialized");
        // Initialize time to 8:00 AM (480 minutes)
        _currentTimeMinutes = 8.0f * 60.0f;
        TimeChanged?.Invoke(_currentTimeMinutes);
        DateChanged?.Invoke(_currentMonth, _currentYear);
    }
    
    public override void _Process(double delta)
    {
        // Advance time: delta seconds * speed multiplier = minutes passed
        // 1 second = 1 minute at normal speed
        float minutesPassed = (float)delta * _timeSpeed;
        float previousTime = _currentTimeMinutes;
        _currentTimeMinutes += minutesPassed;
        
        // Check if we've passed midnight (1440 minutes)
        if (_currentTimeMinutes >= 1440.0f)
        {
            _currentTimeMinutes -= 1440.0f;
            _monthAdvancedToday = false; // Reset flag at midnight
        }
        
        // Check if we've passed 8:00 AM (480 minutes) and haven't advanced month today
        float eightAM = 8.0f * 60.0f;
        if (!_monthAdvancedToday && _currentTimeMinutes >= eightAM && previousTime < eightAM)
        {
            AdvanceMonth();
            _monthAdvancedToday = true;
        }
        
        TimeChanged?.Invoke(_currentTimeMinutes);
    }
    
    public void SlowDownTime()
    {
        if (_currentSpeedIndex > 0)
        {
            _currentSpeedIndex--;
            _timeSpeed = _speedMultipliers[_currentSpeedIndex];
            Log.Debug("TimeManager: Time slowed down to {Speed}x speed", _timeSpeed);
        }
    }
    
    public void SpeedUpTime()
    {
        if (_currentSpeedIndex < _speedMultipliers.Length - 1)
        {
            _currentSpeedIndex++;
            _timeSpeed = _speedMultipliers[_currentSpeedIndex];
            Log.Debug("TimeManager: Time sped up to {Speed}x speed", _timeSpeed);
        }
    }
    
    public void NextMonth()
    {
        // Set time to 8:00 AM and advance month
        _currentTimeMinutes = 8.0f * 60.0f;
        _monthAdvancedToday = true;
        AdvanceMonth();
    }
    
    public void PreviousMonth()
    {
        // Set time to 8:00 AM and go back month
        _currentTimeMinutes = 8.0f * 60.0f;
        _monthAdvancedToday = true;
        if (_currentMonth > 1)
        {
            _currentMonth--;
        }
        else
        {
            _currentMonth = 12;
            _currentYear--;
        }
        DateChanged?.Invoke(_currentMonth, _currentYear);
        Log.Debug("TimeManager: Month changed to {Month}/{Year}", _currentMonth, _currentYear);
    }
    
    private void AdvanceMonth()
    {
        if (_currentMonth < 12)
        {
            _currentMonth++;
        }
        else
        {
            _currentMonth = 1;
            _currentYear++;
        }
        DateChanged?.Invoke(_currentMonth, _currentYear);
        Log.Debug("TimeManager: Month advanced to {Month}/{Year}", _currentMonth, _currentYear);
    }
    
    public string GetTimeString()
    {
        int hours = (int)(_currentTimeMinutes / 60.0f);
        int minutes = (int)(_currentTimeMinutes % 60.0f);
        return $"{hours:D2}:{minutes:D2}";
    }
    
    public string GetMonthString()
    {
        return new DateTime(_currentYear, _currentMonth, 1).ToString("MMMM");
    }
    
    /// <summary>
    /// Gets sunrise time in minutes for the current month
    /// Realistic times: June (5:00), December (8:00), interpolated for others
    /// </summary>
    public float GetSunriseTime()
    {
        // Realistic sunrise times by month (in minutes since midnight)
        // June (month 6): 5:00 = 300 minutes
        // December (month 12): 8:00 = 480 minutes
        // Interpolate for other months
        
        float[] sunriseTimes = {
            480.0f, // January (8:00)
            450.0f, // February (7:30)
            420.0f, // March (7:00)
            360.0f, // April (6:00)
            330.0f, // May (5:30)
            300.0f, // June (5:00) - earliest
            330.0f, // July (5:30)
            360.0f, // August (6:00)
            390.0f, // September (6:30)
            420.0f, // October (7:00)
            450.0f, // November (7:30)
            480.0f  // December (8:00) - latest
        };
        
        int monthIndex = _currentMonth - 1; // Convert to 0-based index
        if (monthIndex >= 0 && monthIndex < sunriseTimes.Length)
        {
            return sunriseTimes[monthIndex];
        }
        
        // Default to 6:00 if month is invalid
        return 360.0f;
    }
    
    /// <summary>
    /// Gets sunset time in minutes for the current month
    /// Realistic times: June (21:00), December (16:00), interpolated for others
    /// </summary>
    public float GetSunsetTime()
    {
        // Realistic sunset times by month (in minutes since midnight)
        // June (month 6): 21:00 = 1260 minutes
        // December (month 12): 16:00 = 960 minutes
        // Interpolate for other months
        
        float[] sunsetTimes = {
            960.0f,  // January (16:00)
            990.0f,  // February (16:30)
            1020.0f, // March (17:00)
            1080.0f, // April (18:00)
            1140.0f, // May (19:00)
            1260.0f, // June (21:00) - latest
            1230.0f, // July (20:30)
            1170.0f, // August (19:30)
            1110.0f, // September (18:30)
            1050.0f, // October (17:30)
            990.0f,  // November (16:30)
            960.0f   // December (16:00) - earliest
        };
        
        int monthIndex = _currentMonth - 1; // Convert to 0-based index
        if (monthIndex >= 0 && monthIndex < sunsetTimes.Length)
        {
            return sunsetTimes[monthIndex];
        }
        
        // Default to 18:00 if month is invalid
        return 1080.0f;
    }
    
    /// <summary>
    /// Gets the current season based on month
    /// </summary>
    public enum Season
    {
        Winter, // Dec, Jan, Feb
        Spring, // Mar, Apr, May
        Summer, // Jun, Jul, Aug
        Autumn  // Sep, Oct, Nov
    }
    
    public Season GetCurrentSeason()
    {
        int month = _currentMonth;
        
        if (month == 12 || month <= 2)
        {
            return Season.Winter;
        }
        else if (month >= 3 && month <= 5)
        {
            return Season.Spring;
        }
        else if (month >= 6 && month <= 8)
        {
            return Season.Summer;
        }
        else // month >= 9 && month <= 11
        {
            return Season.Autumn;
        }
    }
    
    /// <summary>
    /// Gets the twig scale multiplier based on current month with linear interpolation
    /// January (1) = 0%, April (4) = 25%, July (7) = 100%, October (10) = 50%
    /// Linearly interpolates between these key months
    /// </summary>
    public float GetTwigScaleMultiplier()
    {
        int month = _currentMonth;
        
        // Key points: month -> percentage
        // January (1) = 0%
        // April (4) = 25%
        // July (7) = 100%
        // October (10) = 50%
        // Back to January (1) = 0% (wraps around)
        
        if (month == 1)
        {
            return 0.0f; // January = 0%
        }
        else if (month >= 1 && month < 4)
        {
            // January (1) to April (4): 0% to 25%
            float t = (month - 1) / 3.0f; // 0.0 at Jan, 1.0 at Apr
            return Mathf.Lerp(0.0f, 0.25f, t);
        }
        else if (month == 4)
        {
            return 0.25f; // April = 25%
        }
        else if (month > 4 && month < 7)
        {
            // April (4) to July (7): 25% to 100%
            float t = (month - 4) / 3.0f; // 0.0 at Apr, 1.0 at Jul
            return Mathf.Lerp(0.25f, 1.0f, t);
        }
        else if (month == 7)
        {
            return 1.0f; // July = 100%
        }
        else if (month > 7 && month < 10)
        {
            // July (7) to October (10): 100% to 50%
            float t = (month - 7) / 3.0f; // 0.0 at Jul, 1.0 at Oct
            return Mathf.Lerp(1.0f, 0.5f, t);
        }
        else if (month == 10)
        {
            return 0.5f; // October = 50%
        }
        else // month > 10 (Nov, Dec) or month == 12
        {
            // October (10) to January (1): 50% to 0%
            // For Nov (11): t = 1/3, Dec (12): t = 2/3
            float t = (month - 10) / 3.0f; // 0.0 at Oct, 1.0 at Jan (next year)
            return Mathf.Lerp(0.5f, 0.0f, t);
        }
    }
}

