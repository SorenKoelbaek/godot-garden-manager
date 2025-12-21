using GardenManager.Auth;
using GardenManager.Models;
using Godot;

/// <summary>
/// Manages switching between procedural and advanced sky systems, and coordinates sun position updates.
/// </summary>
public class SkyManager
{
	private Environment _environment;
	private TimeManager _timeManager;
	private Node _parentNode;
	private AdvancedSkyRenderer _advancedSkyRenderer;
	private Sky _proceduralSky;
	private ProceduralSkyMaterial _proceduralSkyMaterial;
	private bool _useAdvancedSky = false;
	private float _lastSunElevation = float.NaN;
	private float _lastSunAzimuth = float.NaN;

	/// <summary>
	/// Initializes the SkyManager with the given environment, time manager, and parent node.
	/// Loads settings to determine which sky type to use initially.
	/// </summary>
	/// <param name="environment">The environment to apply the sky to</param>
	/// <param name="timeManager">Time manager for time-based sky updates</param>
	/// <param name="parentNode">Parent node to attach AdvancedSkyRenderer to</param>
	public SkyManager(Environment environment, TimeManager timeManager, Node parentNode)
	{
		_environment = environment;
		_timeManager = timeManager;
		_parentNode = parentNode;

		GD.Print("SkyManager: Initializing");

		var credentialManager = new CredentialManager();
		var settings = credentialManager.LoadSettings();
		bool useAdvanced = settings?.UseAdvancedSky ?? false;

		InitializeSky(useAdvanced);
	}

	/// <summary>
	/// Initializes the sky system with the specified type (advanced or procedural).
	/// </summary>
	/// <param name="useAdvanced">True to use advanced sky, false for procedural sky</param>
	public void InitializeSky(bool useAdvanced)
	{
		_useAdvancedSky = useAdvanced;

		if (useAdvanced)
		{
			InitializeAdvancedSky();
		}
		else
		{
			InitializeProceduralSky();
		}
	}

	/// <summary>
	/// Initializes the procedural sky system with default colors.
	/// </summary>
	private void InitializeProceduralSky()
	{
		GD.Print("SkyManager: Initializing procedural sky");

		if (_proceduralSky == null)
		{
			_proceduralSky = new Sky();
			_proceduralSkyMaterial = new ProceduralSkyMaterial();
			_proceduralSkyMaterial.SkyTopColor = new Color(0.5f, 0.7f, 1.0f);
			_proceduralSkyMaterial.SkyHorizonColor = new Color(0.7f, 0.8f, 0.9f);
			_proceduralSkyMaterial.GroundBottomColor = new Color(0.2f, 0.3f, 0.4f);
			_proceduralSkyMaterial.GroundHorizonColor = new Color(0.5f, 0.6f, 0.7f);
			_proceduralSky.SkyMaterial = _proceduralSkyMaterial;
		}

		if (_advancedSkyRenderer != null)
		{
			_advancedSkyRenderer.Cleanup();
			_advancedSkyRenderer.QueueFree();
			_advancedSkyRenderer = null;
		}

		_environment.Sky = _proceduralSky;
		_environment.BackgroundMode = Environment.BGMode.Sky;

		GD.Print("SkyManager: Procedural sky initialized");
	}

	/// <summary>
	/// Initializes the advanced sky system using a custom sky shader.
	/// </summary>
	private void InitializeAdvancedSky()
	{
		GD.Print("SkyManager: Initializing advanced sky");

		if (_advancedSkyRenderer == null && _parentNode != null)
		{
			_advancedSkyRenderer = new AdvancedSkyRenderer();
			_parentNode.AddChild(_advancedSkyRenderer);
			_advancedSkyRenderer.ForceInitialize();
		}

		if (_advancedSkyRenderer != null && _advancedSkyRenderer.Sky != null)
		{
			_environment.Sky = _advancedSkyRenderer.Sky;
			_environment.BackgroundMode = Environment.BGMode.Sky;
			GD.Print($"SkyManager: Set environment.Sky to advanced sky renderer's Sky");
			GD.Print($"SkyManager: Environment background mode set to Sky: {_environment.BackgroundMode}");

			GD.Print($"SkyManager: Advanced sky initialized - sun position controlled by DirectionalLight3D (current: elev={_lastSunElevation}, azim={_lastSunAzimuth})");
		}
		else
		{
			GD.PrintErr("SkyManager: Advanced sky renderer Sky is null!");
		}

		GD.Print("SkyManager: Advanced sky initialized");
	}

	/// <summary>
	/// Switches between advanced and procedural sky types.
	/// </summary>
	/// <param name="useAdvanced">True to switch to advanced sky, false for procedural sky</param>
	public void SwitchSkyType(bool useAdvanced)
	{
		GD.Print($"SkyManager: Switching sky type to {(useAdvanced ? "advanced" : "procedural")}");

		if (_useAdvancedSky == useAdvanced)
		{
			GD.Print("SkyManager: Sky type already set, skipping switch");
			return;
		}

		_useAdvancedSky = useAdvanced;

		if (useAdvanced)
		{
			InitializeAdvancedSky();
		}
		else
		{
			InitializeProceduralSky();
		}

		if (!float.IsNaN(_lastSunElevation) && !float.IsNaN(_lastSunAzimuth))
		{
			UpdateSunPosition(_lastSunElevation, _lastSunAzimuth);
		}
	}

	/// <summary>
	/// Updates the sun position for the current sky system.
	/// </summary>
	/// <param name="elevation">Sun elevation in degrees</param>
	/// <param name="azimuth">Sun azimuth in degrees</param>
	public void UpdateSunPosition(float elevation, float azimuth)
	{
		_lastSunElevation = elevation;
		_lastSunAzimuth = azimuth;

		if (_useAdvancedSky)
		{
			UpdateAdvancedSkySun(elevation, azimuth);
		}
		else
		{
			UpdateProceduralSkySun(elevation, azimuth);
		}
	}

	/// <summary>
	/// Updates sun position for advanced sky. The shader uses LIGHT0_DIRECTION automatically,
	/// so this method does nothing but is kept for API consistency.
	/// </summary>
	/// <param name="elevation">Sun elevation in degrees (unused)</param>
	/// <param name="azimuth">Sun azimuth in degrees (unused)</param>
	private void UpdateAdvancedSkySun(float elevation, float azimuth)
	{
	}

	/// <summary>
	/// Updates sun position for procedural sky. Procedural sky colors are updated separately by WorldManager.
	/// </summary>
	/// <param name="elevation">Sun elevation in degrees (unused)</param>
	/// <param name="azimuth">Sun azimuth in degrees (unused)</param>
	private void UpdateProceduralSkySun(float elevation, float azimuth)
	{
	}

	/// <summary>
	/// Updates the colors of the procedural sky material.
	/// </summary>
	/// <param name="skyTop">Color for the top of the sky</param>
	/// <param name="skyHorizon">Color for the sky horizon</param>
	/// <param name="groundBottom">Color for the bottom of the ground</param>
	/// <param name="groundHorizon">Color for the ground horizon</param>
	public void UpdateProceduralSkyColors(Color skyTop, Color skyHorizon, Color groundBottom, Color groundHorizon)
	{
		if (_proceduralSkyMaterial != null && !_useAdvancedSky)
		{
			_proceduralSkyMaterial.SkyTopColor = skyTop;
			_proceduralSkyMaterial.SkyHorizonColor = skyHorizon;
			_proceduralSkyMaterial.GroundBottomColor = groundBottom;
			_proceduralSkyMaterial.GroundHorizonColor = groundHorizon;
		}
	}

	/// <summary>
	/// Returns whether the advanced sky system is currently being used.
	/// </summary>
	/// <returns>True if advanced sky is active, false if procedural sky is active</returns>
	public bool IsUsingAdvancedSky()
	{
		return _useAdvancedSky;
	}

	/// <summary>
	/// Cleans up resources by removing and freeing the advanced sky renderer.
	/// </summary>
	public void Cleanup()
	{
		if (_advancedSkyRenderer != null)
		{
			_advancedSkyRenderer.Cleanup();
			if (_advancedSkyRenderer.GetParent() != null)
			{
				_advancedSkyRenderer.GetParent().RemoveChild(_advancedSkyRenderer);
			}
			_advancedSkyRenderer.QueueFree();
			_advancedSkyRenderer = null;
		}
	}
}
