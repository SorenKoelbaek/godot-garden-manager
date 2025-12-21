using System.Collections.Generic;
using System.Linq;
using GardenManager.Api;
using GardenManager.Auth;
using GardenManager.Managers;
using GardenManager.Models;
using Godot;
using Serilog;

public partial class WorldManager : Node3D
{
	private GameManager _gameManager;
	private GardenService _gardenService;
	private ApiClient _apiClient;
	private MeshInstance3D _groundPlane;
	private Label3D _gardenLabel;
	private LoadingSpinner _loadingSpinner;
	private LoadingProgressTracker _progressTracker;
	private Player _player;
	private GameHUD _gameHUD;
	private Node3D _gardenNode;
	private Environment _savedEnvironment;
	private DirectionalLight3D _savedDirectionalLight;
	private DirectionalLight3D _sunLight;
	private Environment _currentEnvironment;
	private TimeManager _timeManager;
	private SkyManager _skyManager;
	// Store each tree's base twig scale (100% value) so seasonal changes are relative to each tree's individual base
	private Dictionary<Node3D, float> _fruitTreeBaseTwigScales = new Dictionary<Node3D, float>();

	public override void _Ready()
	{
		Log.Debug("WorldManager: _Ready() called");
		
		// Get singletons
		_gameManager = GetNode<GameManager>("/root/GameManager");
		_apiClient = GetNode<ApiClient>("/root/ApiClient");
		_gardenService = new GardenService(_apiClient);
		_timeManager = GetNode<TimeManager>("/root/TimeManager");
		
		// Create sun light
		_sunLight = new DirectionalLight3D();
		_sunLight.Name = "SunLight";
		_sunLight.ShadowEnabled = true;
		_sunLight.Visible = true;
		AddChild(_sunLight);
		
		// Set up environment with sky and fog (this is the "menu lighting")
		_currentEnvironment = new Environment();
		_currentEnvironment.BackgroundMode = Environment.BGMode.Sky;
		
		// Create a simple sky
		var sky = new Sky();
		var skyMaterial = new ProceduralSkyMaterial();
		skyMaterial.SkyTopColor = new Color(0.5f, 0.7f, 1.0f);
		skyMaterial.SkyHorizonColor = new Color(0.7f, 0.8f, 0.9f);
		skyMaterial.GroundBottomColor = new Color(0.2f, 0.3f, 0.4f);
		skyMaterial.GroundHorizonColor = new Color(0.5f, 0.6f, 0.7f);
		sky.SkyMaterial = skyMaterial;
		_currentEnvironment.Sky = sky;
		
		// Add fog
		_currentEnvironment.VolumetricFogEnabled = true;
		_currentEnvironment.VolumetricFogDensity = 0.02f;
		
		// Save this as the menu lighting
		SaveCurrentLighting();
		
		// Apply environment to viewport (will be updated by time-of-day system)
		GetViewport().World3D.Environment = _currentEnvironment;
		
		// Initialize sky manager
		_skyManager = new SkyManager(_currentEnvironment, _timeManager, this);
		
		Log.Debug("WorldManager: Environment set up with sky and fog");
		
		// Get references to UI and player
		// WorldManager is attached to MainWorld, and UICanvas is a child of MainWorld
		var uiCanvas = GetNodeOrNull<CanvasLayer>("UICanvas");
		if (uiCanvas != null)
		{
			_loadingSpinner = uiCanvas.GetNodeOrNull<LoadingSpinner>("LoadingSpinner");
			_gameHUD = uiCanvas.GetNodeOrNull<GameHUD>("GameHUD");
			
			Log.Debug("WorldManager: Found UICanvas - spinner: {SpinnerFound}, HUD: {HUDFound}", _loadingSpinner != null, _gameHUD != null);
			
			// Ensure HUD is hidden during loading
			if (_gameHUD != null)
			{
				_gameHUD.Visible = false;
			}
			else
			{
				Log.Error("WorldManager: GameHUD not found in UICanvas!");
			}
			
			// Ensure spinner is visible and spinning
			if (_loadingSpinner != null)
			{
				_loadingSpinner.Visible = true;
				// Initialize progress tracker
				_progressTracker = new LoadingProgressTracker(_loadingSpinner);
			}
			else
			{
				Log.Error("WorldManager: LoadingSpinner not found in UICanvas!");
			}
		}
		else
		{
			Log.Error("WorldManager: UICanvas not found! Path: UICanvas");
		}
		
		_player = GetNodeOrNull<Player>("Player");
		
		// Hide player initially and disable input
		if (_player != null)
		{
			_player.Visible = false;
			_player.SetProcess(false);
			_player.SetPhysicsProcess(false);
		}
		
		// Disable input globally while loading
		GetViewport().GuiDisableInput = true;
		
		// Load and display garden
		_ = LoadGardenAsync();
	}

	private async System.Threading.Tasks.Task LoadGardenAsync()
	{
		var gardenUuid = _gameManager.CurrentGardenUuid;
		Log.Information("WorldManager: Loading garden: {GardenUuid}", gardenUuid);
		
		// Reset progress tracker
		if (_progressTracker != null)
		{
			_progressTracker.Reset();
		}
		
		// Initial progress
		if (_progressTracker != null)
		{
			_progressTracker.SetProgress(0.0f, "Initializing garden...");
		}
		await GetTree().ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		
		GardenManager.Models.Garden gardenData;
		
		if (string.IsNullOrEmpty(gardenUuid))
		{
			Log.Warning("WorldManager: No garden UUID set - creating default plane");
			// Create a default garden for testing
			gardenData = new GardenManager.Models.Garden
			{
				GardenUuid = "default",
				Name = "Default Garden",
				Width = 10,
				Depth = 10,
				Unit = "meters"
			};
		}
		else
		{
			if (_progressTracker != null)
			{
				_progressTracker.SetProgress(2.0f, "Loading garden data...");
			}
			await GetTree().ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			
			gardenData = await _gardenService.GetGardenAsync(gardenUuid);
			if (gardenData == null)
			{
				Log.Error("WorldManager: Failed to load garden - creating default plane");
				gardenData = new GardenManager.Models.Garden
				{
					GardenUuid = gardenUuid,
					Name = "Default Garden",
					Width = 10,
					Depth = 10,
					Unit = "meters"
				};
			}
		}

		Log.Information("WorldManager: Garden loaded: {GardenName}, Size: {Width}x{Depth} {Unit}", gardenData.Name, gardenData.Width, gardenData.Depth, gardenData.Unit);
		
		// Load and instantiate the Garden scene (Node3D, not the API model)
		var gardenScene = GD.Load<PackedScene>("res://scenes/world/garden.tscn");
		if (gardenScene != null)
		{
			var gardenInstance = gardenScene.Instantiate();
			if (gardenInstance != null && gardenInstance is Node3D gardenNode)
			{
				// Get the Garden script component and call SetSize
				if (gardenNode.HasMethod("SetSize"))
				{
					gardenNode.Call("SetSize", (float)gardenData.Width, (float)gardenData.Depth, gardenData.Unit);
				}
				
				gardenNode.Name = "Garden";
				_gardenNode = gardenNode;
				AddChild(gardenNode);
				
				// Get the ground plane reference for other operations
				_groundPlane = gardenNode.GetNode<MeshInstance3D>("GroundPlane");
				
				Log.Debug("WorldManager: Created garden from scene");
				
				// Wait for garden to be ready (grass generation)
				await WaitForGardenReady(gardenNode);
				
				// Apply grass setting from saved settings
				ApplyGrassSetting(gardenNode);
			}
			else
			{
				Log.Error("WorldManager: Failed to instantiate garden scene - falling back to manual creation");
				CreateGardenPlane(gardenData);
				CreateGardenPerimeter(gardenData);
			}
		}
		else
		{
			Log.Error("WorldManager: Failed to load garden scene - falling back to manual creation");
			CreateGardenPlane(gardenData);
			CreateGardenPerimeter(gardenData);
		}
		
		CreateGardenPerimeter(gardenData);
		
		// Load and render plots - await completion before finishing loading
		await LoadPlotsAsync(gardenUuid, gardenData);
		
		// Finalize loading - show player and HUD, hide spinner
		FinishLoading();
		
		// Subscribe to time changes for sun/sky updates and tree season updates
		if (_timeManager != null)
		{
			_timeManager.TimeChanged += OnTimeChanged;
			_timeManager.DateChanged += OnDateChanged;
		}
	}
	
	public override void _Process(double delta)
	{
		// Update sun position and sky colors based on time
		if (_timeManager != null && _sunLight != null && _sunLight.Visible)
		{
			UpdateSunPosition();
			UpdateSkyColors();
		}
	}
	
	private int _lastTreeUpdateHour = -1;
	
	private void OnTimeChanged(float timeMinutes)
	{
		// Update trees every hour for smooth transitions as month progresses
		// Since a full day cycle represents a month, we update trees hourly
		if (_fruitTreeBaseTwigScales.Count == 0)
		{
			return; // No trees to update
		}
		
		// Check if we've crossed an hour boundary
		int currentHour = (int)(timeMinutes / 60.0f);
		if (_lastTreeUpdateHour != currentHour)
		{
			UpdateAllTreesForSeason();
			_lastTreeUpdateHour = currentHour;
		}
	}
	
	private void OnDateChanged(int month, int year)
	{
		// Date changed - update sun position and tree seasons
		UpdateAllTreesForSeason();
	}
	
	private void UpdateAllTreesForSeason()
	{
		if (_timeManager == null)
		{
			return;
		}
		
		// Get month-based values (linearly interpolated)
		float monthTwigMultiplier = _timeManager.GetTwigScaleMultiplier();
		int currentMonth = _timeManager.CurrentMonth;
		
		// Get month-based color with linear interpolation
		Color twigColor = GetTwigColorForMonth(_timeManager.CurrentMonth);
		
		// Load base twig material
		var baseTwigMaterial = GD.Load<Material>("res://resources/plot_types/twig_mat.tres");
		if (baseTwigMaterial == null)
		{
			Log.Error("WorldManager: Failed to load base twig material for season update!");
			return;
		}
		
		// Update each tree
		foreach (var kvp in _fruitTreeBaseTwigScales.ToList())
		{
			var tree3D = kvp.Key;
			float baseTwigScale = kvp.Value; // This is the tree's 100% twig scale value
			
			// Check if tree still exists
			if (tree3D == null || !IsInstanceValid(tree3D))
			{
				_fruitTreeBaseTwigScales.Remove(tree3D);
				continue;
			}
			
			// Apply seasonal multiplier to the tree's base twig scale
			// monthTwigMultiplier is 0.0 (0%) to 1.0 (100%), so this gives us the seasonal percentage
			float newTwigScale = baseTwigScale * monthTwigMultiplier;
			tree3D.Set("twig_scale", newTwigScale);
			
			// Update twig material color
			var currentTwigMaterial = tree3D.Get("material_twig");
			if (currentTwigMaterial.VariantType != Variant.Type.Nil && currentTwigMaterial.AsGodotObject() is StandardMaterial3D existingMaterial)
			{
				existingMaterial.AlbedoColor = twigColor;
			}
			else
			{
				// Create new material if needed
				var newTwigMaterial = baseTwigMaterial.Duplicate() as StandardMaterial3D;
				if (newTwigMaterial != null)
				{
					newTwigMaterial.AlbedoColor = twigColor;
					tree3D.Set("material_twig", newTwigMaterial);
				}
			}
		}
		
		Log.Debug("WorldManager: Updated {TreeCount} trees for month {Month} (twig multiplier: {Multiplier:F2})", _fruitTreeBaseTwigScales.Count, currentMonth, monthTwigMultiplier);
	}
	
	/// <summary>
	/// Gets twig color for a given month with linear interpolation
	/// January (1) = muted, April (4) = light green, July (7) = full green, October (10) = orange/brown
	/// </summary>
	private Color GetTwigColorForMonth(int month)
	{
		// Key color points
		Color janColor = new Color(0.4f, 0.4f, 0.3f);      // Muted (winter)
		Color aprColor = new Color(0.5f, 0.8f, 0.3f);      // Light green (spring)
		Color julColor = new Color(0.3f, 0.6f, 0.2f);     // Full green (summer)
		Color octColor = new Color(0.8f, 0.5f, 0.2f);     // Orange/brown (autumn)
		
		if (month == 1)
		{
			return janColor;
		}
		else if (month >= 1 && month < 4)
		{
			// January (1) to April (4)
			float t = (month - 1) / 3.0f;
			return janColor.Lerp(aprColor, t);
		}
		else if (month == 4)
		{
			return aprColor;
		}
		else if (month > 4 && month < 7)
		{
			// April (4) to July (7)
			float t = (month - 4) / 3.0f;
			return aprColor.Lerp(julColor, t);
		}
		else if (month == 7)
		{
			return julColor;
		}
		else if (month > 7 && month < 10)
		{
			// July (7) to October (10)
			float t = (month - 7) / 3.0f;
			return julColor.Lerp(octColor, t);
		}
		else if (month == 10)
		{
			return octColor;
		}
		else // month > 10 (Nov, Dec)
		{
			// October (10) to January (1)
			float t = (month - 10) / 3.0f;
			return octColor.Lerp(janColor, t);
		}
	}
	
	private async System.Threading.Tasks.Task WaitForGardenReady(Node3D gardenNode)
	{
		Log.Debug("WorldManager: Waiting for garden to be ready...");
		
		// Poll until garden is ready
		int maxAttempts = 300; // 5 seconds max wait (300 * ~16ms)
		int attempts = 0;
		
		while (attempts < maxAttempts)
		{
			// Try to get the Garden script and check IsReady property
			// Check directly via the Garden class if we can cast
			try
			{
				if (gardenNode is Garden garden)
				{
					if (garden.IsReady)
					{
						Log.Debug("WorldManager: Garden is ready!");
						return;
					}
				}
			}
			catch (System.Exception ex)
			{
				Log.Error(ex, "WorldManager: Error checking garden ready state");
			}
			
			// Wait a frame before checking again
			await GetTree().ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			attempts++;
		}
		
		Log.Error("WorldManager: Timeout waiting for garden to be ready");
	}
	
	private void ApplyGrassSetting(Node3D gardenNode)
	{
		// Load settings and apply grass visibility
		var credentialManager = new CredentialManager();
		var settings = credentialManager.LoadSettings();
		
		if (settings != null && gardenNode is Garden garden)
		{
			garden.SetGrassVisible(settings.RenderGrass);
			Log.Debug("WorldManager: Applied grass setting: {RenderGrass}", settings.RenderGrass);
		}
		else if (settings == null)
		{
			// Default to visible if no settings found
			if (gardenNode is Garden defaultGarden)
			{
				defaultGarden.SetGrassVisible(true);
				Log.Debug("WorldManager: No settings found, defaulting grass to visible");
			}
		}
	}
	
	private void FinishLoading()
	{
		Log.Debug("WorldManager: Finishing loading - showing player and HUD");
		
		// Re-enable input globally
		GetViewport().GuiDisableInput = false;
		
		// Hide spinner FIRST
		if (_loadingSpinner != null)
		{
			Log.Debug("WorldManager: Hiding spinner (spinner is null: {IsNull})", _loadingSpinner == null);
			_loadingSpinner.Visible = false;
			_loadingSpinner.HideSpinner();
			Log.Debug("WorldManager: Spinner hidden, visible state: {Visible}", _loadingSpinner.Visible);
		}
		else
		{
			Log.Error("WorldManager: LoadingSpinner is null! Cannot hide it.");
		}
		
		// Show player and enable input
		if (_player != null)
		{
			_player.Visible = true;
			_player.SetProcess(true);
			_player.SetPhysicsProcess(true);
		}
		
		// Show HUD
		if (_gameHUD != null)
		{
			_gameHUD.ShowHUD();
		}
		else
		{
			Log.Error("WorldManager: GameHUD is null! Cannot show it.");
		}
		
		Log.Information("WorldManager: Loading complete");
	}
	
	private void SaveCurrentLighting()
	{
		Log.Debug("WorldManager: Saving current lighting state for menu restoration");
		
		// Save current environment (deep copy)
		if (_currentEnvironment != null)
		{
			_savedEnvironment = _currentEnvironment.Duplicate() as Environment;
		}
		
		// Find and save any existing DirectionalLight3D
		var existingLight = GetTree().CurrentScene.GetNodeOrNull<DirectionalLight3D>("DirectionalLight3D");
		if (existingLight != null)
		{
			_savedDirectionalLight = existingLight;
		}
		else
		{
			// Create a default directional light for menu (if none exists)
			_savedDirectionalLight = new DirectionalLight3D();
			_savedDirectionalLight.Position = new Vector3(5, 10, 5);
			// Use look_at_from_position since node is not in tree yet
			_savedDirectionalLight.LookAtFromPosition(new Vector3(5, 10, 5), Vector3.Zero);
			_savedDirectionalLight.LightColor = new Color(1.0f, 1.0f, 0.95f);
			_savedDirectionalLight.LightEnergy = 1.0f;
		}
	}
	
	public void RestoreMenuLighting()
	{
		Log.Debug("WorldManager: Restoring menu lighting");
		
		// Restore saved environment
		if (_savedEnvironment != null)
		{
			GetViewport().World3D.Environment = _savedEnvironment;
		}
		
		// Ensure the saved directional light is visible
		if (_savedDirectionalLight != null && _savedDirectionalLight.GetParent() == null)
		{
			GetTree().CurrentScene.AddChild(_savedDirectionalLight);
		}
		
		// Hide sun light if it exists
		if (_sunLight != null)
		{
			_sunLight.Visible = false;
		}
	}
	
	public void RestoreTimeBasedLighting()
	{
		Log.Debug("WorldManager: Restoring time-based lighting");
		
		// Show sun light
		if (_sunLight != null)
		{
			_sunLight.Visible = true;
		}
		
		// Hide saved directional light
		if (_savedDirectionalLight != null && _savedDirectionalLight.GetParent() != null)
		{
			_savedDirectionalLight.GetParent().RemoveChild(_savedDirectionalLight);
		}
	}
	
	private void UpdateSunPosition()
	{
		if (_timeManager == null || _sunLight == null)
		{
			return;
		}
		
		float currentTime = _timeManager.CurrentTimeMinutes;
		float sunriseTime = _timeManager.GetSunriseTime();
		float sunsetTime = _timeManager.GetSunsetTime();
		
		// Calculate sun elevation (0° at sunrise/sunset, 90° at noon)
		float sunElevation = 0.0f;
		float sunAzimuth = 0.0f; // 0° = East, 90° = South, 180° = West
		
		// Determine if it's day or night
		bool isDay = currentTime >= sunriseTime && currentTime <= sunsetTime;
		
		if (isDay)
		{
			// Calculate position in day cycle (0.0 = sunrise, 1.0 = sunset)
			float dayLength = sunsetTime - sunriseTime;
			float timeInDay = currentTime - sunriseTime;
			float dayProgress = timeInDay / dayLength;
			
			// Sun elevation: 0° at sunrise/sunset, peaks at noon (90°)
			// Use a sine curve for smooth arc
			sunElevation = Mathf.Sin(dayProgress * Mathf.Pi) * 90.0f;
			
			// Adjust elevation by month (higher in summer, lower in winter)
			// June = +15°, December = -15°
			float monthAdjustment = 0.0f;
			int month = _timeManager.CurrentMonth;
			if (month == 6) // June
			{
				monthAdjustment = 15.0f;
			}
			else if (month == 12) // December
			{
				monthAdjustment = -15.0f;
			}
			else
			{
				// Interpolate between months
				if (month < 6)
				{
					// January to May: interpolate from -15° to +15°
					monthAdjustment = -15.0f + (month + 5) * (30.0f / 10.0f);
				}
				else
				{
					// July to December: interpolate from +15° to -15°
					monthAdjustment = 15.0f - (month - 6) * (30.0f / 6.0f);
				}
			}
			
			sunElevation += monthAdjustment;
			sunElevation = Mathf.Clamp(sunElevation, -90.0f, 90.0f);
			
			// Sun azimuth: 0° (East) at sunrise, 180° (West) at sunset
			sunAzimuth = dayProgress * 180.0f;
		}
		else
		{
			// Night: sun is below horizon
			sunElevation = -10.0f; // Slightly below horizon
			
			// Determine if it's before sunrise or after sunset
			if (currentTime < sunriseTime)
			{
				// Before sunrise: sun is in the east (0°)
				sunAzimuth = 0.0f;
			}
			else
			{
				// After sunset: sun is in the west (180°)
				sunAzimuth = 180.0f;
			}
		}
		
		// Convert elevation and azimuth to rotation
		// In Godot: X rotation = elevation (pitch), Y rotation = azimuth (yaw)
		// Elevation: -90° (down) to +90° (up)
		// Azimuth: 0° (East) to 180° (West)
		float elevationRad = Mathf.DegToRad(sunElevation);
		float azimuthRad = Mathf.DegToRad(sunAzimuth);
		
		// Rotate the light to point at the sun position
		// DirectionalLight3D points in -Z direction by default
		// We need to rotate it to point toward the sun
		_sunLight.Rotation = new Vector3(
			-elevationRad, // Negative because we want to look up for positive elevation
			azimuthRad - Mathf.Pi / 2.0f, // Adjust for Godot's coordinate system (0° = -Z, 90° = -X)
			0.0f
		);
		
		// Update light intensity based on elevation
		// Brighter at noon, dimmer at dawn/dusk, very dim at night
		float intensity = 1.0f;
		if (sunElevation > 0.0f)
		{
			// Day: intensity based on elevation (0.3 at horizon, 1.0 at 90°)
			intensity = 0.3f + (sunElevation / 90.0f) * 0.7f;
		}
		else
		{
			// Night: very dim
			intensity = 0.1f;
		}
		
		_sunLight.LightEnergy = intensity;
		
		// Update light color based on time of day
		if (sunElevation < 10.0f && sunElevation > -5.0f)
		{
			// Dawn/dusk: warm orange
			_sunLight.LightColor = new Color(1.0f, 0.7f, 0.5f);
		}
		else if (sunElevation > 10.0f)
		{
			// Day: white/yellow
			_sunLight.LightColor = new Color(1.0f, 1.0f, 0.95f);
		}
		else
		{
			// Night: cool blue
			_sunLight.LightColor = new Color(0.5f, 0.6f, 0.8f);
		}
		
		// Update sky manager with sun position
		// This sun position is calculated from:
		// - Current time (minutes) from TimeManager
		// - Sunrise/sunset times (which vary by month)
		// - Month-based elevation adjustments (higher in summer, lower in winter)
		// The advanced sky renderer will use this to render the sky with correct sun position
		if (_skyManager != null)
		{
			_skyManager.UpdateSunPosition(sunElevation, sunAzimuth);
		}
	}
	
	private void UpdateSkyColors()
	{
		if (_timeManager == null || _currentEnvironment == null)
		{
			return;
		}
		
		// Only update procedural sky colors if using procedural sky
		// Advanced sky handles its own updates
		if (_skyManager != null && _skyManager.IsUsingAdvancedSky())
		{
			// Using advanced sky, skip procedural color updates
			return;
		}
		
		// Get sky material for procedural sky
		var skyMaterial = _currentEnvironment.Sky?.SkyMaterial as ProceduralSkyMaterial;
		if (skyMaterial == null)
		{
			return;
		}
		
		float currentTime = _timeManager.CurrentTimeMinutes;
		float sunriseTime = _timeManager.GetSunriseTime();
		float sunsetTime = _timeManager.GetSunsetTime();
		
		// Calculate sun elevation for color interpolation
		float sunElevation = 0.0f;
		bool isDay = currentTime >= sunriseTime && currentTime <= sunsetTime;
		
		if (isDay)
		{
			float dayLength = sunsetTime - sunriseTime;
			float timeInDay = currentTime - sunriseTime;
			float dayProgress = timeInDay / dayLength;
			sunElevation = Mathf.Sin(dayProgress * Mathf.Pi) * 90.0f;
			
			// Adjust by month
			int month = _timeManager.CurrentMonth;
			float monthAdjustment = 0.0f;
			if (month == 6)
				monthAdjustment = 15.0f;
			else if (month == 12)
				monthAdjustment = -15.0f;
			else if (month < 6)
				monthAdjustment = -15.0f + (month + 5) * (30.0f / 10.0f);
			else
				monthAdjustment = 15.0f - (month - 6) * (30.0f / 6.0f);
			
			sunElevation += monthAdjustment;
		}
		else
		{
			sunElevation = -10.0f; // Below horizon
		}
		
		// Define color states
		Color dawnSkyTop = new Color(1.0f, 0.6f, 0.4f);      // Warm orange/pink
		Color dawnSkyHorizon = new Color(1.0f, 0.5f, 0.3f);
		Color daySkyTop = new Color(0.3f, 0.6f, 1.0f);       // Blue sky
		Color daySkyHorizon = new Color(0.7f, 0.8f, 0.9f);
		Color duskSkyTop = new Color(1.0f, 0.4f, 0.2f);      // Orange/red
		Color duskSkyHorizon = new Color(1.0f, 0.3f, 0.1f);
		Color nightSkyTop = new Color(0.05f, 0.05f, 0.15f);  // Dark blue/purple
		Color nightSkyHorizon = new Color(0.1f, 0.1f, 0.2f);
		
		Color groundBottom = new Color(0.2f, 0.3f, 0.4f);
		Color groundHorizon = new Color(0.5f, 0.6f, 0.7f);
		
		Color skyTop, skyHorizon;
		
		// Interpolate colors based on sun elevation
		if (sunElevation > 20.0f)
		{
			// Day: blue sky
			skyTop = daySkyTop;
			skyHorizon = daySkyHorizon;
		}
		else if (sunElevation > 0.0f)
		{
			// Dawn/dusk transition: interpolate between dawn and day, or day and dusk
			float t = sunElevation / 20.0f;
			if (currentTime < sunriseTime + 60.0f || currentTime > sunsetTime - 60.0f)
			{
				// Near sunrise/sunset: dawn/dusk colors
				if (currentTime < sunriseTime + 60.0f)
				{
					// Dawn
					skyTop = dawnSkyTop.Lerp(daySkyTop, t);
					skyHorizon = dawnSkyHorizon.Lerp(daySkyHorizon, t);
				}
				else
				{
					// Dusk
					skyTop = daySkyTop.Lerp(duskSkyTop, 1.0f - t);
					skyHorizon = daySkyHorizon.Lerp(duskSkyHorizon, 1.0f - t);
				}
			}
			else
			{
				// Day
				skyTop = daySkyTop;
				skyHorizon = daySkyHorizon;
			}
		}
		else if (sunElevation > -10.0f)
		{
			// Just below horizon: dawn/dusk
			if (currentTime < sunriseTime)
			{
				// Pre-dawn
				float t = (sunElevation + 10.0f) / 10.0f;
				skyTop = nightSkyTop.Lerp(dawnSkyTop, t);
				skyHorizon = nightSkyHorizon.Lerp(dawnSkyHorizon, t);
			}
			else
			{
				// Post-dusk
				float t = (sunElevation + 10.0f) / 10.0f;
				skyTop = duskSkyTop.Lerp(nightSkyTop, 1.0f - t);
				skyHorizon = duskSkyHorizon.Lerp(nightSkyHorizon, 1.0f - t);
			}
		}
		else
		{
			// Night: dark
			skyTop = nightSkyTop;
			skyHorizon = nightSkyHorizon;
		}
		
		// Update sky material (only for procedural sky)
		if (_skyManager != null)
		{
			_skyManager.UpdateProceduralSkyColors(skyTop, skyHorizon, groundBottom, groundHorizon);
		}
		else
		{
			// Fallback if sky manager not initialized
			skyMaterial.SkyTopColor = skyTop;
			skyMaterial.SkyHorizonColor = skyHorizon;
			skyMaterial.GroundBottomColor = groundBottom;
			skyMaterial.GroundHorizonColor = groundHorizon;
		}
	}
	
	public void SetAdvancedSky(bool enabled)
	{
		Log.Debug("WorldManager: Setting advanced sky to {Enabled}", enabled);
		if (_skyManager != null)
		{
			_skyManager.SwitchSkyType(enabled);
			
		// Update sun position immediately after switching
		// This ensures the advanced sky gets the correct sun position based on current time and month
		if (_timeManager != null)
		{
			UpdateSunPosition(); // This calculates elevation/azimuth from time-of-day and month, then updates sky manager
		}
		}
		else
		{
			Log.Error("WorldManager: SkyManager not initialized, cannot set advanced sky");
		}
	}

	/// <summary>
	/// Loads all plots for the garden and creates their 3D representations.
	/// Uses LoadingProgressTracker to show progress.
	/// 
	/// To add a new plot type:
	/// 1. Filter plots by type (like vegetablePlots/fruitTreePlots)
	/// 2. Count how many you'll create
	/// 3. Register a phase: _progressTracker.RegisterPhase("NewPlotType", weight)
	/// 4. Loop through and create them, calling _progressTracker.UpdateItemProgress() for each
	/// 5. Call _progressTracker.CompletePhase() when done
	/// 
	/// The progress tracker automatically handles the math - remaining 90% is split proportionally
	/// based on the weights you assign to each plot type phase.
	/// </summary>
	private async System.Threading.Tasks.Task LoadPlotsAsync(string gardenUuid, GardenManager.Models.Garden garden)
	{
		Log.Debug("WorldManager: Loading plots for garden: {GardenUuid}", gardenUuid);
		
		if (_progressTracker == null)
		{
			Log.Error("WorldManager: Progress tracker not initialized!");
			return;
		}
		
		// Register loading phases
		// Phase 1: Getting plots from API = 10% (always)
		_progressTracker.RegisterPhase("GettingPlots", 10.0f);
		
		// Update progress: Getting plots
		_progressTracker.SetProgress(0.0f, "Loading plots...");
		await GetTree().ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		
		var plots = await _gardenService.GetPlotsAsync(gardenUuid);
		Log.Debug("WorldManager: Loaded {PlotCount} plots", plots.Count);
		
		// Complete getting plots phase
		_progressTracker.CompletePhase("GettingPlots", "Plots loaded");
		await GetTree().ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		
		// Filter plots by type
		var vegetablePlots = plots.Where(p => 
			p.PlotType != null && 
			p.PlotType.Name.ToLower().Contains("vegetable")).ToList();
		
		var fruitTreePlots = plots.Where(p => 
			p.PlotType != null && 
			p.PlotType.Name.ToLower().Contains("fruit tree")).ToList();
		
		Log.Debug("WorldManager: Found {VegetableCount} vegetable plots, {FruitTreeCount} fruit tree plots", vegetablePlots.Count, fruitTreePlots.Count);
		
		// Count only rectangle vegetable plots (the ones we actually create)
		int vegetablePlotCount = vegetablePlots.Count(p => p.Shape.ToLower() == "rectangle");
		
		// Register phases for each plot type
		// Remaining 90% is split equally among all items
		int totalItems = vegetablePlotCount + fruitTreePlots.Count;
		
		if (totalItems > 0)
		{
			// Register phases dynamically based on what we need to create
			if (vegetablePlotCount > 0)
			{
				_progressTracker.RegisterPhase("VegetablePlots", 90.0f * vegetablePlotCount / totalItems);
			}
			
			if (fruitTreePlots.Count > 0)
			{
				_progressTracker.RegisterPhase("FruitTrees", 90.0f * fruitTreePlots.Count / totalItems);
			}
		}
		else
		{
			// No items to load, mark as complete
			_progressTracker.SetProgress(100.0f, "No plots to load");
			return;
		}
		
		Log.Debug("WorldManager: Registered phases - VegetablePlots: {VegetableCount} items, FruitTrees: {FruitTreeCount} items", vegetablePlotCount, fruitTreePlots.Count);
		
		// Yield frame after filtering
		await GetTree().ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		
		// Render each vegetable plot
		int vegetablePlotIndex = 0;
		foreach (var plot in vegetablePlots)
		{
			if (plot.Shape.ToLower() == "rectangle")
			{
				vegetablePlotIndex++;
				_progressTracker.UpdateItemProgress("VegetablePlots", vegetablePlotIndex, vegetablePlotCount, plot.Name);
				CreatePlotRectangle(plot, garden);
				// Yield frame after each plot to prevent framerate drop
				await GetTree().ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			}
		}
		
		// Complete vegetable plots phase if we processed any
		if (vegetablePlotCount > 0)
		{
			_progressTracker.CompletePhase("VegetablePlots");
			await GetTree().ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		
		// Render each fruit tree plot with frequent yields
		int treeIndex = 0;
		foreach (var plot in fruitTreePlots)
		{
			treeIndex++;
			_progressTracker.UpdateItemProgress("FruitTrees", treeIndex, fruitTreePlots.Count, plot.Name);
			await CreateFruitTreeAsync(plot, garden);
			// Yield frame after each tree to prevent framerate drop (trees are heavy)
			await GetTree().ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		
		// Complete fruit trees phase if we processed any
		if (fruitTreePlots.Count > 0)
		{
			_progressTracker.CompletePhase("FruitTrees");
			await GetTree().ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		
		// Ensure we're at 100% when done
		_progressTracker.SetProgress(100.0f, "Loading complete!");
		await GetTree().ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
	}

	/// <summary>
	/// Converts garden coordinates to 3D world coordinates.
	/// Garden coordinate system: X (0 to width) from left, Y (0 to depth) from top.
	/// 3D coordinate system: centered at origin, X left-right, Z forward-back (-Z is forward).
	/// </summary>
	/// <param name="gardenX">X coordinate in garden space (distance from left edge)</param>
	/// <param name="gardenY">Y coordinate in garden space (distance from top edge)</param>
	/// <param name="garden">The garden object with dimensions</param>
	/// <returns>3D position (X, Z) representing the upper-left corner of the plot</returns>
	private Vector2 ConvertGardenToWorldCoordinates(double gardenX, double gardenY, GardenManager.Models.Garden garden)
	{
		// Convert garden dimensions to meters if needed
		float gardenWidth = (float)garden.Width;
		float gardenDepth = (float)garden.Depth;
		
		if (garden.Unit == "feet")
		{
			gardenWidth *= 0.3048f;
			gardenDepth *= 0.3048f;
		}
		
		// Garden X: 0 to gardenWidth (left to right, distance from left edge)
		// 3D X: -gardenWidth/2 to +gardenWidth/2 (centered at origin)
		// Left edge of garden in 3D = -gardenWidth/2
		// So: 3D X = -gardenWidth/2 + gardenX = gardenX - gardenWidth/2
		float worldX = (float)gardenX - gardenWidth / 2.0f;
		
		// Garden Y: 0 to gardenDepth (top to bottom, distance from top edge)
		// 3D Z: -gardenDepth/2 to +gardenDepth/2 (centered at origin, -Z is forward/top)
		// Top edge of garden in 3D = -gardenDepth/2 (because -Z is forward/top)
		// So: 3D Z = -gardenDepth/2 + gardenY = gardenY - gardenDepth/2
		float worldZ = (float)gardenY - gardenDepth / 2.0f;
		
		return new Vector2(worldX, worldZ);
	}

	private void CreatePlotRectangle(Plot plot, GardenManager.Models.Garden garden)
	{
		Log.Debug("WorldManager: Creating rectangle plot: {PlotName} at ({X}, {Y}), size: {Width}x{Depth}", plot.Name, plot.X, plot.Y, plot.Width, plot.Depth);
		
		// Convert plot dimensions to meters if needed
		float width = (float)plot.Width;
		float depth = (float)plot.Depth;
		
		if (plot.Unit == "feet")
		{
			width *= 0.3048f;
			depth *= 0.3048f;
		}
		
		// Convert garden dimensions to meters for coordinate conversion
		float gardenWidth = (float)garden.Width;
		float gardenDepth = (float)garden.Depth;
		
		if (garden.Unit == "feet")
		{
			gardenWidth *= 0.3048f;
			gardenDepth *= 0.3048f;
		}
		
		// Convert garden coordinates to 3D world coordinates (upper-left corner)
		var plotUpperLeft = ConvertGardenToWorldCoordinates(plot.X, plot.Y, garden);
		float plotUpperLeftX = plotUpperLeft.X;
		float plotUpperLeftZ = plotUpperLeft.Y;
		
		// Calculate center position
		float plotCenterX = plotUpperLeftX + width / 2.0f;
		float plotCenterZ = plotUpperLeftZ + depth / 2.0f;
		
		// Try to load VegetablePlot scene, fallback to simple box if not found
		var vegetablePlotScene = GD.Load<PackedScene>("res://scenes/assets/vegetable_plot.tscn");
		if (vegetablePlotScene != null)
		{
			var plotInstance = vegetablePlotScene.Instantiate();
			if (plotInstance != null && plotInstance is VegetablePlot vegetablePlot)
			{
				// Set plot identification
				vegetablePlot.PlotUuid = plot.PlotUuid;
				vegetablePlot.PlotName = plot.Name;
				
				// Set size before positioning
				vegetablePlot.SetSize(width, depth);
				
				// Position at plot location
				vegetablePlot.Position = new Vector3(plotCenterX, 0, plotCenterZ);
				
				// Apply rotation (rotation is in radians, around Y axis)
				vegetablePlot.Rotation = new Vector3(0, (float)plot.Rotation, 0);
				
				AddChild(vegetablePlot);
				
				Log.Debug("WorldManager: Created vegetable plot prefab - upper-left corner at ({UpperLeftX}, {UpperLeftZ}), center at ({CenterX}, 0, {CenterZ}), size: {Width}x{Depth}", plotUpperLeftX, plotUpperLeftZ, plotCenterX, plotCenterZ, width, depth);
				return;
			}
		}
		
		// Fallback to simple box if scene not found
		Log.Warning("WorldManager: VegetablePlot scene not found, using simple box fallback");
		
		// Create box mesh for the plot (cube/box with height 0.35m)
		var boxMesh = new BoxMesh();
		boxMesh.Size = new Vector3(width, 0.35f, depth);
		
		var plotMesh = new MeshInstance3D();
		plotMesh.Mesh = boxMesh;
		
		// Position at plot location, with box centered at half height
		plotMesh.Position = new Vector3(plotCenterX, 0.35f / 2.0f, plotCenterZ);
		
		// Apply rotation (rotation is in radians, around Y axis)
		plotMesh.Rotation = new Vector3(0, (float)plot.Rotation, 0);
		
		// Create material - brown color for plots
		var material = new StandardMaterial3D();
		material.AlbedoColor = new Color(0.6f, 0.4f, 0.2f); // Brown color
		plotMesh.MaterialOverride = material;
		
		AddChild(plotMesh);
		
		// Add collision shape for the plot box
		var staticBody = new StaticBody3D();
		var collisionShape = new CollisionShape3D();
		var boxShape = new BoxShape3D();
		boxShape.Size = new Vector3(width, 0.35f, depth);
		collisionShape.Shape = boxShape;
		collisionShape.Position = new Vector3(plotCenterX, 0.35f / 2.0f, plotCenterZ);
		collisionShape.Rotation = new Vector3(0, (float)plot.Rotation, 0);
		staticBody.AddChild(collisionShape);
		AddChild(staticBody);
		
		Log.Debug("WorldManager: Created plot box - upper-left corner at ({UpperLeftX}, {UpperLeftZ}), center at ({CenterX}, {CenterY}, {CenterZ}) with collision", plotUpperLeftX, plotUpperLeftZ, plotCenterX, 0.35f / 2.0f, plotCenterZ);
	}

	private async System.Threading.Tasks.Task CreateFruitTreeAsync(Plot plot, GardenManager.Models.Garden garden)
	{
		// Yield frame at start to allow spinner to update
		await GetTree().ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		
		Log.Debug("WorldManager: Creating fruit tree plot: {PlotName} at ({X}, {Y}), size: {Width}x{Depth}", plot.Name, plot.X, plot.Y, plot.Width, plot.Depth);
		
		// Convert garden coordinates to 3D world coordinates (upper-left corner)
		var plotUpperLeft = ConvertGardenToWorldCoordinates(plot.X, plot.Y, garden);
		float plotUpperLeftX = plotUpperLeft.X;
		float plotUpperLeftZ = plotUpperLeft.Y;
		
		// Position tree at center of plot
		// Convert plot dimensions to meters if needed
		float plotWidth = (float)plot.Width;
		float plotDepth = (float)plot.Depth;
		
		if (plot.Unit == "feet")
		{
			plotWidth *= 0.3048f;
			plotDepth *= 0.3048f;
		}
		
		// Yield frame after coordinate calculations
		await GetTree().ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		
		// Calculate scale factor based on average of width and depth
		// Average gives us a representative size for the plot
		float plotSizeAverage = (plotWidth + plotDepth) / 2.0f;
		
		// Base scale: assume 1m average = base tree size
		// Scale factor: how much larger/smaller than base
		// Make trees 5x smaller overall
		float scaleFactor = (plotSizeAverage / 1.0f) / 5.0f;
		
		// Clamp scale factor to reasonable bounds (0.1x to 0.6x after 5x reduction)
		scaleFactor = Mathf.Clamp(scaleFactor, 0.1f, 0.6f);
		
		// Center of plot: upper-left corner + half width/depth
		// X: upper-left X + half width = center X
		float treeX = plotUpperLeftX + plotWidth / 2.0f;
		// Z: upper-left Z + half depth = center Z
		// Since upper-left is at -Z (forward/top), we add depth/2 to get center
		float treeZ = plotUpperLeftZ + plotDepth / 2.0f;
		
		// Yield frame before loading materials
		await GetTree().ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		
		// Load the materials
		var trunkMaterial = GD.Load<Material>("res://resources/plot_types/trunk_mat.tres");
		var baseTwigMaterial = GD.Load<Material>("res://resources/plot_types/twig_mat.tres");
		
		if (trunkMaterial == null || baseTwigMaterial == null)
		{
			Log.Error("WorldManager: Failed to load tree materials!");
			return;
		}
		
		// Yield frame after loading materials
		await GetTree().ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		
		// Create season-based twig material by duplicating the base material
		var twigMaterial = baseTwigMaterial.Duplicate() as StandardMaterial3D;
		if (twigMaterial == null)
		{
			Log.Error("WorldManager: Failed to duplicate twig material!");
			return;
		}
		
		// Get month-based color (linearly interpolated)
		Color twigColor = GetTwigColorForMonth(_timeManager != null ? _timeManager.CurrentMonth : 7);
		if (_timeManager != null)
		{
			Log.Debug("WorldManager: Setting twig color to {TwigColor} for month {Month}", twigColor, _timeManager.CurrentMonth);
		}
		
		// Apply the color to the material
		twigMaterial.AlbedoColor = twigColor;
		
		// Yield frame before loading tree type
		await GetTree().ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		
		// Get random tree type
		var treeType = TreeTypeLoader.GetRandomTreeType();
		if (treeType == null)
		{
			Log.Error("WorldManager: Failed to get random tree type!");
			return;
		}
		
		Log.Debug("WorldManager: Selected tree type: {TreeTypeName}", treeType.Name);
		
		// Yield frame before instantiating tree
		await GetTree().ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		
		// Try to instantiate Tree3D using ClassDB
		var className = new StringName("Tree3D");
		if (ClassDB.CanInstantiate(className))
		{
			var tree3DVariant = ClassDB.Instantiate(className);
			var tree3D = tree3DVariant.AsGodotObject() as Node3D;
			
			if (tree3D != null)
			{
				// Yield frame after instantiation
				await GetTree().ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				
				// Load materials from tree type or use defaults
				Material? finalTrunkMaterial = trunkMaterial;
				Material? finalTwigMaterial = twigMaterial;
				
				if (!string.IsNullOrEmpty(treeType.MaterialTrunk))
				{
					var customTrunkMaterial = GD.Load<Material>(treeType.MaterialTrunk);
					if (customTrunkMaterial != null)
					{
						finalTrunkMaterial = customTrunkMaterial;
					}
				}
				
				if (!string.IsNullOrEmpty(treeType.MaterialTwig))
				{
					var customTwigMaterial = GD.Load<Material>(treeType.MaterialTwig);
					if (customTwigMaterial != null)
					{
						// Duplicate and apply seasonal color
						var customTwigMat = customTwigMaterial.Duplicate() as StandardMaterial3D;
						if (customTwigMat != null)
						{
							customTwigMat.AlbedoColor = twigColor;
							finalTwigMaterial = customTwigMat;
						}
					}
				}
				
				// Yield frame before setting properties
				await GetTree().ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				
				// Set materials
				tree3D.Set("material_trunk", finalTrunkMaterial);
				tree3D.Set("material_twig", finalTwigMaterial);
				
				// Determine seed: use tree type's seed if provided, otherwise use plot UUID hash
				int seed;
				if (treeType.Seed.HasValue)
				{
					seed = treeType.Seed.Value;
					// Add plot UUID hash to seed for variation per plot while keeping tree type consistent
					seed = (seed + (int)(plot.PlotUuid.GetHashCode() % 1000)) % 10000;
					if (seed < 0) seed = -seed;
				}
				else
				{
					seed = (int)(plot.PlotUuid.GetHashCode() % 10000);
					if (seed < 0) seed = -seed;
				}
				
				// Get month-based twig scale multiplier (linearly interpolated)
				float monthTwigMultiplier = 1.0f;
				if (_timeManager != null)
				{
					monthTwigMultiplier = _timeManager.GetTwigScaleMultiplier();
				}
				
				// Yield frame before setting many properties
				await GetTree().ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				
				// Apply tree type properties with plot size scaling where appropriate
				tree3D.Set("seed", seed);
				
				// Properties that should scale with plot size
				if (treeType.TrunkHeight.HasValue)
				{
					tree3D.Set("trunk_height", (int)(treeType.TrunkHeight.Value * scaleFactor));
				}
				
				if (treeType.TrunkMaxRadius.HasValue)
				{
					tree3D.Set("trunk_max_radius", treeType.TrunkMaxRadius.Value * scaleFactor);
				}
				
				if (treeType.TrunkLength.HasValue)
				{
					tree3D.Set("trunk_length", treeType.TrunkLength.Value * scaleFactor);
				}
				
				if (treeType.TrunkBranchLength.HasValue)
				{
					tree3D.Set("trunk_branch_length", treeType.TrunkBranchLength.Value * scaleFactor);
				}
				
				// Yield frame mid-way through property setting
				await GetTree().ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				
				// Properties that don't scale with plot size (ratios, angles, etc.)
				if (treeType.TrunkBranchesCount.HasValue)
				{
					tree3D.Set("trunk_branches_count", treeType.TrunkBranchesCount.Value);
				}
				
				if (treeType.TrunkBranchLengthFalloff.HasValue)
				{
					tree3D.Set("trunk_branch_length_falloff", treeType.TrunkBranchLengthFalloff.Value);
				}
				
				if (treeType.TrunkRadiusFalloffRate.HasValue)
				{
					tree3D.Set("trunk_radius_falloff_rate", treeType.TrunkRadiusFalloffRate.Value);
				}
				
				if (treeType.TrunkTwist.HasValue)
				{
					tree3D.Set("trunk_twist", treeType.TrunkTwist.Value);
				}
				
				if (treeType.TrunkKink.HasValue)
				{
					tree3D.Set("trunk_kink", treeType.TrunkKink.Value);
				}
				
				if (treeType.TrunkClimbRate.HasValue)
				{
					tree3D.Set("trunk_climb_rate", treeType.TrunkClimbRate.Value);
				}
				
				if (treeType.TrunkDropAmount.HasValue)
				{
					tree3D.Set("trunk_drop_amount", treeType.TrunkDropAmount.Value);
				}
				
				if (treeType.TrunkGrowAmount.HasValue)
				{
					tree3D.Set("trunk_grow_amount", treeType.TrunkGrowAmount.Value);
				}
				
				// Yield frame before final calculations
				await GetTree().ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				
				// Calculate the tree's base twig scale (100% value) from tree type
				// This is what the tree would have at full foliage, scaled by plot size
				float treeBaseTwigScale = (treeType.TwigScale ?? 0.6f) * scaleFactor;
				
				// Apply seasonal multiplier to get current twig scale
				// monthTwigMultiplier is 0.0 (0%) to 1.0 (100%), relative to the tree's base
				tree3D.Set("twig_scale", treeBaseTwigScale * monthTwigMultiplier);
				
				// Position the tree at center of plot
				tree3D.Position = new Vector3(treeX, 0, treeZ);
				
				// Yield frame before adding to scene tree
				await GetTree().ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				
				AddChild(tree3D);
				
				// Store tree's base twig scale (100% value) for seasonal updates
				// This allows seasonal percentages to be relative to each tree's individual base
				_fruitTreeBaseTwigScales[tree3D] = treeBaseTwigScale;
				
				Log.Debug("WorldManager: Created fruit tree '{TreeTypeName}' at position ({TreeX}, 0, {TreeZ}) with seed {Seed}, scale factor: {ScaleFactor:F2} (plot size avg: {PlotSizeAvg:F2}m)", treeType.Name, treeX, treeZ, seed, scaleFactor, plotSizeAverage);
			}
			else
			{
				Log.Error("WorldManager: Failed to instantiate Tree3D node!");
			}
		}
		else
		{
			Log.Error("WorldManager: Cannot instantiate Tree3D class! Make sure the GDExtension is loaded.");
		}
	}

	private void CreateGardenPlane(GardenManager.Models.Garden garden)
	{
		// Convert dimensions to meters if needed
		float width = (float)garden.Width;
		float depth = (float)garden.Depth;
		
		if (garden.Unit == "feet")
		{
			// Convert feet to meters (1 foot = 0.3048 meters)
			width *= 0.3048f;
			depth *= 0.3048f;
		}

		// Create plane mesh
		// PlaneMesh in Godot: X is left-right, Y is forward-back (Z in 3D)
		// Garden: longest dimension is left-right (X), shorter is up-down (Y in garden, Z in 3D)
		var planeMesh = new PlaneMesh();
		planeMesh.Size = new Vector2(width, depth);
		
		_groundPlane = new MeshInstance3D();
		_groundPlane.Mesh = planeMesh;
		// Center the garden at origin
		_groundPlane.Position = new Vector3(0, 0, 0);
		
		// Create material with grass texture
		var material = new StandardMaterial3D();
		material.AlbedoColor = new Color(0.3f, 0.7f, 0.3f); // Light green base color
		
		// Try to load a grass texture if available, otherwise use solid color
		var grassTexture = GD.Load<Texture2D>("res://resources/grass.png");
		if (grassTexture == null)
		{
			// Try alternative paths
			grassTexture = GD.Load<Texture2D>("res://grass.png");
		}
		
		if (grassTexture != null)
		{
			material.AlbedoTexture = grassTexture;
			// Set UV scale to tile the texture across the garden
			// Scale texture based on garden size (tile every 2 meters)
			// Use the larger dimension to ensure proper tiling
			float textureScale = Mathf.Max(width, depth) / 2.0f;
			material.Uv1Scale = new Vector3(textureScale, textureScale, 1.0f);
			// Enable texture filtering for better quality
			material.TextureFilter = BaseMaterial3D.TextureFilterEnum.Linear;
			Log.Debug("WorldManager: Applied grass texture with scale {TextureScale}", textureScale);
		}
		else
		{
			Log.Debug("WorldManager: No grass texture found, using solid green color");
			Log.Debug("WorldManager: To add grass texture, place grass.png in resources/ folder");
		}
		
		_groundPlane.MaterialOverride = material;
		
		AddChild(_groundPlane);
		
		// Add collision shape for the ground
		var staticBody = new StaticBody3D();
		var collisionShape = new CollisionShape3D();
		var boxShape = new BoxShape3D();
		boxShape.Size = new Vector3(width, 0.1f, depth);
		collisionShape.Shape = boxShape;
		collisionShape.Position = new Vector3(0, -0.05f, 0);
		staticBody.AddChild(collisionShape);
		AddChild(staticBody);
		
		// Create label to show garden UUID
		_gardenLabel = new Label3D();
		_gardenLabel.Text = $"Garden UUID: {garden.GardenUuid}";
		_gardenLabel.Position = new Vector3(0, 2.0f, 0);
		_gardenLabel.Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
		AddChild(_gardenLabel);
		
		// Add lighting
		var directionalLight = new DirectionalLight3D();
		directionalLight.Position = new Vector3(5, 10, 5);
		directionalLight.LookAt(Vector3.Zero);
		directionalLight.LightColor = new Color(1.0f, 1.0f, 0.95f);
		AddChild(directionalLight);
		
		Log.Debug("WorldManager: Created garden plane: {Width}m x {Depth}m with lighting and collision", width, depth);
	}

	private void CreateGardenPerimeter(GardenManager.Models.Garden garden)
	{
		// Convert dimensions to meters if needed
		float width = (float)garden.Width;
		float depth = (float)garden.Depth;
		
		if (garden.Unit == "feet")
		{
			width *= 0.3048f;
			depth *= 0.3048f;
		}

		const float wallHeight = 3.0f; // 3 meters wall height
		const float wallThickness = 0.1f; // 10cm wall thickness

		// Create material for walls - translucent
		var wallMaterial = new StandardMaterial3D();
		wallMaterial.AlbedoColor = new Color(0.5f, 0.4f, 0.3f, 0.5f); // Brownish wall color, 50% transparent
		wallMaterial.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;

		// Garden is centered at origin, so it goes from:
		// X: -width/2 to +width/2
		// Z: -depth/2 to +depth/2
		
		// Position walls OUTSIDE the garden bounds
		// North wall (top, -Z direction) - positioned outside the north edge
		CreateWallSegment(
			new Vector3(0, wallHeight / 2.0f, -depth / 2.0f - wallThickness / 2.0f),
			new Vector3(width + wallThickness * 2, wallHeight, wallThickness),
			wallMaterial
		);

		// South wall (bottom, +Z direction) - positioned outside the south edge
		CreateWallSegment(
			new Vector3(0, wallHeight / 2.0f, depth / 2.0f + wallThickness / 2.0f),
			new Vector3(width + wallThickness * 2, wallHeight, wallThickness),
			wallMaterial
		);

		// West wall (left, -X direction) - positioned outside the west edge
		CreateWallSegment(
			new Vector3(-width / 2.0f - wallThickness / 2.0f, wallHeight / 2.0f, 0),
			new Vector3(wallThickness, wallHeight, depth + wallThickness * 2),
			wallMaterial
		);

		// East wall (right, +X direction) - positioned outside the east edge
		CreateWallSegment(
			new Vector3(width / 2.0f + wallThickness / 2.0f, wallHeight / 2.0f, 0),
			new Vector3(wallThickness, wallHeight, depth + wallThickness * 2),
			wallMaterial
		);

		Log.Debug("WorldManager: Created garden perimeter walls outside garden bounds: {Width}m x {Depth}m", width, depth);
	}

	// CreateGrass is no longer needed - grass is now part of the Garden scene

	private void CreateWallSegment(Vector3 position, Vector3 size, StandardMaterial3D material)
	{
		var wallMesh = new BoxMesh();
		wallMesh.Size = size;

		var wallInstance = new MeshInstance3D();
		wallInstance.Mesh = wallMesh;
		wallInstance.Position = position;
		wallInstance.MaterialOverride = material;
		wallInstance.Visible = false; // Make walls invisible but keep collision

		AddChild(wallInstance);

		// Add collision for the wall
		var staticBody = new StaticBody3D();
		var collisionShape = new CollisionShape3D();
		var boxShape = new BoxShape3D();
		boxShape.Size = size;
		collisionShape.Shape = boxShape;
		collisionShape.Position = position;
		staticBody.AddChild(collisionShape);
		AddChild(staticBody);
	}
}
