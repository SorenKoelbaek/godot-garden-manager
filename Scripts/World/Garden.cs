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
		Image img = Image.Create(256, 256, false, Image.Format.L8);
		img.Fill(new Color(0.35f, 0f, 0f, 1f)); // 35% grey baseline

		// Convert to ImageTexture
		_runtimeGrowthTexture = ImageTexture.CreateFromImage(img);

		// Assign to shader
		shaderMat.SetShaderParameter("growth_texture", _runtimeGrowthTexture);

		Log.Debug("Garden: Assigned runtime growth_texture to shader.");
	}


	// --------------------------------------------------------------------
	// WIND UPDATES
	// --------------------------------------------------------------------
	public override void _Process(double delta)
	{
		UpdateWindSpeed();
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
}
