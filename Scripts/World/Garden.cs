using Godot;
using Serilog;

public partial class Garden : Node3D
{
	[Export]
	public float Width { get; set; } = 10.0f;

	[Export]
	public float Depth { get; set; } = 10.0f;

	[Export]
	public string Unit { get; set; } = "meters";

	private MeshInstance3D _groundPlane;
	private MultiMeshInstance3D _grassNode;
	private TimeManager _timeManager;

	private float _lastWindSpeed = 1.0f;
	private const float BaseWindSpeed = 1.0f;
	private const float MinWindSpeed = 0.5f;
	private const float MaxWindSpeed = 4.0f;

	private ImageTexture _runtimeGrowthTexture; // Runtime-only texture
	private Image? _growthImage; // Runtime image buffer for growth updates
	private int _lastHour = -1; // Track last hour to detect hour changes


	// --------------------------------------------------------------------
	// CHECK IF READY
	// --------------------------------------------------------------------
	public bool IsReady
	{
		get
		{
			if (_groundPlane == null || _groundPlane.Mesh == null)
				return false;

			if (_grassNode == null || _grassNode.Multimesh == null)
				return false;

			return _grassNode.Multimesh.InstanceCount > 0;
		}
	}


	// --------------------------------------------------------------------
	// GIVE BrushOverlay the runtime growth texture
	// --------------------------------------------------------------------
	public ImageTexture GetGrowthTexture()
	{
		if (_runtimeGrowthTexture == null)
		{
			Log.Error("Garden: runtime growth texture is NULL in GetGrowthTexture()");
		}

		return _runtimeGrowthTexture;
	}


	// --------------------------------------------------------------------
	// READY
	// --------------------------------------------------------------------
	public override void _Ready()
	{
		Log.Debug("Garden: _Ready() called");

		// ---------------------------------------------------------
		// Get nodes
		// ---------------------------------------------------------
		_groundPlane = GetNode<MeshInstance3D>("GroundPlane");
		_grassNode = GetNode<MultiMeshInstance3D>("GroundPlane/Grass");

		if (_groundPlane == null)
		{
			Log.Error("Garden: GroundPlane node not found!");
			return;
		}

		if (_grassNode == null)
		{
			Log.Error("Garden: Grass node not found!");
			return;
		}

		// ---------------------------------------------------------
		// Update mesh size
		// ---------------------------------------------------------
		UpdateGardenSize();

		// ---------------------------------------------------------
		// Force Grass.gd rebuild()
		// ---------------------------------------------------------
		if (_groundPlane.Mesh != null)
		{
			_grassNode.Set("mesh", _groundPlane.Mesh);
			Log.Debug("Garden: Set grass mesh property to GroundPlane mesh (type: {MeshType})", _groundPlane.Mesh.GetClass());
		}

		// ---------------------------------------------------------
		// Create & assign runtime growth texture
		// ---------------------------------------------------------
		AssignRuntimeGrowthTexture();

		// ---------------------------------------------------------
		// Get time manager
		// ---------------------------------------------------------
		_timeManager = GetNode<TimeManager>("/root/TimeManager");
		
		// Subscribe to time changes for hourly grass growth
		if (_timeManager != null)
		{
			_timeManager.TimeChanged += OnTimeChanged;
		}

		Log.Debug("Garden: Initialized");
	}


	// --------------------------------------------------------------------
	// CREATE RUNTIME GROWTH TEXTURE
	// --------------------------------------------------------------------
	private void AssignRuntimeGrowthTexture()
	{
		Log.Debug("Garden: Creating runtime growth texture...");

		var shaderMat = _grassNode.MaterialOverride as ShaderMaterial;
		if (shaderMat == null)
		{
			Log.Error("Garden: Grass material not found! Cannot assign growth texture.");
			return;
		}

		// Create runtime image (L8 format, grayscale)
		_growthImage = Image.Create(256, 256, false, Image.Format.L8);
		_growthImage.Fill(new Color(0.35f, 0f, 0f, 1f)); // 35% grey baseline (0.35 = partially grown)

		// Convert to ImageTexture
		_runtimeGrowthTexture = ImageTexture.CreateFromImage(_growthImage);

		// Assign to shader
		shaderMat.SetShaderParameter("growth_texture", _runtimeGrowthTexture);

		Log.Debug("Garden: Assigned runtime growth_texture to shader (initialized at 35% grey).");
	}


	// --------------------------------------------------------------------
	// WIND UPDATES
	// --------------------------------------------------------------------
	public override void _Process(double delta)
	{
		UpdateWindSpeed();
	}
	
	// --------------------------------------------------------------------
	// TIME CHANGE HANDLER - Hourly grass growth
	// --------------------------------------------------------------------
	private void OnTimeChanged(float timeMinutes)
	{
		if (_timeManager == null || _growthImage == null || _runtimeGrowthTexture == null)
			return;
		
		// Calculate current hour (0-23)
		int currentHour = (int)(timeMinutes / 60.0f) % 24;
		
		// Check if hour has changed
		if (currentHour != _lastHour)
		{
			_lastHour = currentHour;
			GrowGrassHourly();
		}
	}
	
	// --------------------------------------------------------------------
	// GROW GRASS - Make it 5% closer to black (fully grown) each hour
	// --------------------------------------------------------------------
	private void GrowGrassHourly()
	{
		if (_growthImage == null || _runtimeGrowthTexture == null)
			return;
		
		int width = _growthImage.GetWidth();
		int height = _growthImage.GetHeight();
		bool textureUpdated = false;
		
		// Process each pixel
		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				Color currentColor = _growthImage.GetPixel(x, y);
				float currentValue = currentColor.R; // L8 format uses R channel for grayscale
				
				// Move 1% closer to black (0.0 = fully grown)
				// Formula: newValue = currentValue * 0.99 (move 1% towards 0)
				float newValue = currentValue * 0.99f;
				
				// Clamp to ensure we don't go below 0.0
				newValue = Mathf.Max(0.0f, newValue);
				
				// Only update if value changed (optimization)
				if (Mathf.Abs(newValue - currentValue) > 0.001f)
				{
					_growthImage.SetPixel(x, y, new Color(newValue, 0f, 0f, 1f));
					textureUpdated = true;
				}
			}
		}
		
		// Update texture if any pixels changed
		if (textureUpdated)
		{
			_runtimeGrowthTexture.Update(_growthImage);
			Log.Debug("Garden: Grass grew 1% closer to fully grown (hour {Hour})", _lastHour);
		}
	}
	
	// --------------------------------------------------------------------
	// GET GROWTH IMAGE - For BrushOverlay to paint on
	// --------------------------------------------------------------------
	public Image? GetGrowthImage()
	{
		return _growthImage;
	}
	
	// --------------------------------------------------------------------
	// UPDATE GROWTH TEXTURE - Call after modifying growth image
	// --------------------------------------------------------------------
	public void UpdateGrowthTexture()
	{
		if (_growthImage != null && _runtimeGrowthTexture != null)
		{
			_runtimeGrowthTexture.Update(_growthImage);
		}
	}

	private void UpdateWindSpeed()
	{
		if (_timeManager == null || _grassNode == null)
			return;

		float timeMultiplier = _timeManager.TimeSpeed;

		float windSpeed = BaseWindSpeed * timeMultiplier;
		windSpeed = Mathf.Clamp(windSpeed, MinWindSpeed, MaxWindSpeed);

		if (Mathf.Abs(windSpeed - _lastWindSpeed) < 0.01f)
			return;

		_lastWindSpeed = windSpeed;

		var material = _grassNode.MaterialOverride as ShaderMaterial;
		if (material != null)
		{
			material.SetShaderParameter("wind_speed", windSpeed);
			Log.Debug("Garden: Updated wind speed to {WindSpeed:F2}x (time multiplier: {TimeMultiplier:F2}x)", windSpeed, timeMultiplier);
		}
	}


	// --------------------------------------------------------------------
	// SIZE & COLLIDER
	// --------------------------------------------------------------------
	public void SetSize(float width, float depth, string unit = "meters")
	{
		Width = width;
		Depth = depth;
		Unit = unit;

		UpdateGardenSize();
	}

	private void UpdateGardenSize()
	{
		if (_groundPlane == null)
			return;

		float width = Width;
		float depth = Depth;

		if (Unit == "feet")
		{
			width *= 0.3048f;
			depth *= 0.3048f;
		}

		if (_groundPlane.Mesh is PlaneMesh planeMesh)
		{
			planeMesh.Size = new Vector2(width, depth);
		}
		else
		{
			var newPlane = new PlaneMesh();
			newPlane.Size = new Vector2(width, depth);
			_groundPlane.Mesh = newPlane;
		}

		Log.Debug("Garden: Updated plane size to {Width}m x {Depth}m", width, depth);

		// Notify Grass.gd to rebuild instance transforms
		if (_grassNode != null && _groundPlane.Mesh != null)
		{
			_grassNode.Set("mesh", _groundPlane.Mesh);
		}

		UpdateCollisionBody(width, depth);
	}

	private void UpdateCollisionBody(float width, float depth)
	{
		var collisionBody = GetNodeOrNull<StaticBody3D>("GroundPlane/GroundCollision");
		if (collisionBody == null)
			return;

		var colShape = collisionBody.GetChild<CollisionShape3D>(0);
		if (colShape?.Shape is BoxShape3D box)
		{
			box.Size = new Vector3(width, 0.1f, depth);
		}
	}


	// --------------------------------------------------------------------
	// GRASS VISIBILITY CONTROL
	// --------------------------------------------------------------------
	public void SetGrassVisible(bool visible)
	{
		if (_grassNode != null)
		{
			_grassNode.Visible = visible;
			Log.Debug("Garden: Grass visibility set to {Visible}", visible);
		}
		else
		{
			Log.Error("Garden: Cannot set grass visibility - grass node is null");
		}
	}
	
	public override void _ExitTree()
	{
		// Unsubscribe from time changes
		if (_timeManager != null)
		{
			_timeManager.TimeChanged -= OnTimeChanged;
		}
	}
}
