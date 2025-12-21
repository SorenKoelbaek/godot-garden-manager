using Godot;

/// <summary>
/// Renders an advanced sky using a custom sky shader with atmospheric scattering, clouds, stars, and dynamic day/night transitions.
/// The sky shader automatically uses LIGHT0_DIRECTION from the first DirectionalLight3D for sun position.
/// </summary>
public partial class AdvancedSkyRenderer : Node
{
	private Sky _sky;
	private ShaderMaterial _skyMaterial;
	private Texture2D _starsTexture;
	private bool _isInitialized = false;
	
	public bool IsInitialized => _isInitialized;
	public Sky Sky => _sky;

	/// <summary>
	/// Called when the node enters the scene tree. Initializes the sky renderer if not already initialized.
	/// </summary>
	public override void _Ready()
	{
		GD.Print("AdvancedSkyRenderer: _Ready() called");
		if (!_isInitialized)
		{
			Initialize();
		}
	}
	
	/// <summary>
	/// Forces initialization of the sky renderer. Called from SkyManager if _Ready() doesn't fire in time.
	/// </summary>
	public void ForceInitialize()
	{
		GD.Print("AdvancedSkyRenderer: ForceInitialize() called");
		if (!_isInitialized)
		{
			Initialize();
		}
	}

	/// <summary>
	/// Initializes the advanced sky system by creating Sky resource, loading shader, setting up materials, and configuring shader parameters.
	/// </summary>
	private void Initialize()
	{
		if (_isInitialized)
		{
			GD.Print("AdvancedSkyRenderer: Initialize() called but already initialized, skipping");
			return;
		}
		
		GD.Print("AdvancedSkyRenderer: Initialize() called");
		
		_isInitialized = true;
		
		_sky = new Sky();
		if (_sky == null)
		{
			GD.PrintErr("AdvancedSkyRenderer: CRITICAL - Failed to create Sky object!");
			return;
		}
		
		var skyShader = GD.Load<Shader>("res://resources/advanced_sky/sky_shader.gdshader");
		if (skyShader == null)
		{
			GD.PrintErr("AdvancedSkyRenderer: Failed to load sky shader!");
			return;
		}

		_skyMaterial = new ShaderMaterial();
		if (_skyMaterial == null)
		{
			GD.PrintErr("AdvancedSkyRenderer: CRITICAL - Failed to create ShaderMaterial!");
			return;
		}

		_skyMaterial.Shader = skyShader;
		_sky.SkyMaterial = _skyMaterial;
		
		GD.Print($"AdvancedSkyRenderer: Created Sky and SkyMaterial (sky: {_sky != null}, material: {_skyMaterial != null}, shader: {skyShader != null})");

		var starsTexturePath = "res://resources/advanced_sky/milkywaypan_brunier_2048.jpg";
		_starsTexture = GD.Load<Texture2D>(starsTexturePath);
		if (_starsTexture == null)
		{
			GD.PrintErr($"AdvancedSkyRenderer: Failed to load stars texture from {starsTexturePath}!");
		}
		else
		{
			GD.Print($"AdvancedSkyRenderer: Loaded stars texture: {_starsTexture.GetWidth()}x{_starsTexture.GetHeight()}");
			_skyMaterial.SetShaderParameter("stars_texture", _starsTexture);
		}

		_skyMaterial.SetShaderParameter("day_top_color", new Color(0.1f, 0.6f, 1.0f));
		_skyMaterial.SetShaderParameter("day_bottom_color", new Color(0.4f, 0.8f, 1.0f));
		_skyMaterial.SetShaderParameter("sunset_top_color", new Color(0.7f, 0.75f, 1.0f));
		_skyMaterial.SetShaderParameter("sunset_bottom_color", new Color(1.0f, 0.5f, 0.7f));
		_skyMaterial.SetShaderParameter("night_top_color", new Color(0.02f, 0.0f, 0.04f));
		_skyMaterial.SetShaderParameter("night_bottom_color", new Color(0.1f, 0.0f, 0.2f));
		
		_skyMaterial.SetShaderParameter("horizon_color", new Color(0.0f, 0.7f, 0.8f));
		_skyMaterial.SetShaderParameter("horizon_blur", 0.05f);
		
		_skyMaterial.SetShaderParameter("sun_color", new Color(10.0f, 8.0f, 1.0f));
		_skyMaterial.SetShaderParameter("sun_sunset_color", new Color(10.0f, 0.0f, 0.0f));
		_skyMaterial.SetShaderParameter("sun_size", 0.2f);
		_skyMaterial.SetShaderParameter("sun_blur", 10.0f);
		
		_skyMaterial.SetShaderParameter("moon_color", new Color(1.0f, 0.95f, 0.7f));
		_skyMaterial.SetShaderParameter("moon_size", 0.06f);
		_skyMaterial.SetShaderParameter("moon_blur", 0.1f);
		
		_skyMaterial.SetShaderParameter("clouds_edge_color", new Color(0.8f, 0.8f, 0.98f));
		_skyMaterial.SetShaderParameter("clouds_top_color", new Color(1.0f, 1.0f, 1.0f));
		_skyMaterial.SetShaderParameter("clouds_middle_color", new Color(0.92f, 0.92f, 0.98f));
		_skyMaterial.SetShaderParameter("clouds_bottom_color", new Color(0.83f, 0.83f, 0.94f));
		_skyMaterial.SetShaderParameter("clouds_speed", 2.0f);
		_skyMaterial.SetShaderParameter("clouds_direction", 0.2f);
		_skyMaterial.SetShaderParameter("clouds_scale", 1.0f);
		_skyMaterial.SetShaderParameter("clouds_cutoff", 0.3f);
		_skyMaterial.SetShaderParameter("clouds_fuzziness", 0.5f);
		_skyMaterial.SetShaderParameter("clouds_weight", 0.0f);
		_skyMaterial.SetShaderParameter("clouds_blur", 0.25f);
		
		_skyMaterial.SetShaderParameter("stars_speed", 1.0f);
		
		_skyMaterial.SetShaderParameter("overwritten_time", 0.0f);

		GD.Print("AdvancedSkyRenderer: Initialization complete - sky shader is ready");
		GD.Print("AdvancedSkyRenderer: Note - sun position is controlled by DirectionalLight3D (LIGHT0_DIRECTION)");
	}

	/// <summary>
	/// Compatibility method for sun position updates. The sky shader uses LIGHT0_DIRECTION automatically,
	/// so this method does nothing but is kept for API compatibility.
	/// </summary>
	/// <param name="sunDirection">The sun direction vector (unused, kept for compatibility)</param>
	public void UpdateSunPosition(Vector3 sunDirection)
	{
		if (_isInitialized)
		{
			GD.Print($"AdvancedSkyRenderer: Sun direction is {sunDirection} (controlled by DirectionalLight3D)");
		}
	}

	/// <summary>
	/// Cleans up resources by nullifying references to sky objects and textures.
	/// </summary>
	public void Cleanup()
	{
		GD.Print("AdvancedSkyRenderer: Cleaning up");
		_skyMaterial = null;
		_sky = null;
		_starsTexture = null;
	}

	/// <summary>
	/// Called when the node exits the scene tree. Performs cleanup of resources.
	/// </summary>
	public override void _ExitTree()
	{
		Cleanup();
	}
}
