using Godot;

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
    
    /// <summary>
    /// Checks if the garden is ready (plane mesh exists and grass has been generated)
    /// </summary>
    public bool IsReady
    {
        get
        {
            // Check if ground plane mesh exists and is set
            if (_groundPlane == null || _groundPlane.Mesh == null)
            {
                return false;
            }
            
            // Check if grass multimesh has instances
            if (_grassNode == null || _grassNode.Multimesh == null)
            {
                return false;
            }
            
            // Grass is ready when it has instances (instance_count > 0)
            return _grassNode.Multimesh.InstanceCount > 0;
        }
    }
    
    public override void _Ready()
    {
        GD.Print("Garden: _Ready() called");
        
        // Get child nodes
        _groundPlane = GetNode<MeshInstance3D>("GroundPlane");
        _grassNode = GetNode<MultiMeshInstance3D>("GroundPlane/Grass");
        
        if (_groundPlane == null)
        {
            GD.PrintErr("Garden: GroundPlane node not found!");
            return;
        }
        
        if (_grassNode == null)
        {
            GD.PrintErr("Garden: Grass node not found!");
            return;
        }
        
        // Update the garden size first (this ensures the mesh exists and is the right size)
        UpdateGardenSize();
        
        // CRITICAL: Set the grass mesh property to the ACTUAL GroundPlane mesh instance
        // The scene file has mesh = SubResource, but we need to set it to the actual mesh instance
        // that GroundPlane is using, which might be modified at runtime
        if (_groundPlane.Mesh != null)
        {
            // Set the mesh property on the grass node - this triggers rebuild() in grass.gd
            _grassNode.Set("mesh", _groundPlane.Mesh);
            
            // Verify it was set by reading it back
            var meshValue = _grassNode.Get("mesh");
            if (meshValue.VariantType == Variant.Type.Nil || meshValue.AsGodotObject() == null)
            {
                GD.PrintErr("Garden: FAILED to set grass mesh property! Value is null after setting.");
            }
            else
            {
                var meshObj = meshValue.AsGodotObject();
                GD.Print($"Garden: Successfully set grass mesh property (type: {meshObj.GetClass()})");
            }
        }
        else
        {
            GD.PrintErr("Garden: GroundPlane mesh is null! Cannot set grass mesh.");
        }
        
        // Get TimeManager for wind speed updates
        _timeManager = GetNode<TimeManager>("/root/TimeManager");
        
        GD.Print("Garden: Initialized");
    }
    
    public override void _Process(double delta)
    {
        // Update wind speed based on time speed
        UpdateWindSpeed();
    }
    
    private void UpdateWindSpeed()
    {
        if (_timeManager == null || _grassNode == null)
        {
            return;
        }
        
        // Get TimeManager speed multiplier
        float timeMultiplier = _timeManager.TimeSpeed;
        
        // Scale wind speed proportionally: windSpeed = baseSpeed * timeMultiplier
        float windSpeed = BaseWindSpeed * timeMultiplier;
        
        // Cap between 0.5x and 4.0x
        windSpeed = Mathf.Clamp(windSpeed, MinWindSpeed, MaxWindSpeed);
        
        // Only update if changed
        if (Mathf.Abs(windSpeed - _lastWindSpeed) > 0.01f)
        {
            _lastWindSpeed = windSpeed;
            
            // Update grass material shader parameter
            var material = _grassNode.MaterialOverride as ShaderMaterial;
            if (material != null)
            {
                material.SetShaderParameter("wind_speed", windSpeed);
                GD.Print($"Garden: Updated wind speed to {windSpeed:F2}x (time multiplier: {timeMultiplier:F2}x)");
            }
        }
    }
    
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
        {
            return;
        }
        
        // Convert dimensions to meters if needed
        float width = Width;
        float depth = Depth;
        
        if (Unit == "feet")
        {
            width *= 0.3048f;
            depth *= 0.3048f;
        }
        
        // Update the plane mesh size
        if (_groundPlane.Mesh is PlaneMesh planeMesh)
        {
            planeMesh.Size = new Vector2(width, depth);
            GD.Print($"Garden: Updated plane size to {width}m x {depth}m");
        }
        else
        {
            // Create new plane mesh if it doesn't exist
            var newPlaneMesh = new PlaneMesh();
            newPlaneMesh.Size = new Vector2(width, depth);
            _groundPlane.Mesh = newPlaneMesh;
            GD.Print($"Garden: Created new plane mesh with size {width}m x {depth}m");
        }
        
        // Update the grass mesh reference (triggers rebuild in GDScript)
        // This must happen after the plane mesh is updated
        // The grass.gd script needs the mesh property set to the GroundPlane mesh
        if (_grassNode != null && _groundPlane.Mesh != null)
        {
            // Set the mesh property directly - this triggers rebuild() in grass.gd
            _grassNode.Set("mesh", _groundPlane.Mesh);
            GD.Print($"Garden: Set grass mesh property to GroundPlane mesh (type: {_groundPlane.Mesh.GetClass()})");
        }
        else
        {
            GD.PrintErr($"Garden: Cannot set grass mesh - grassNode={_grassNode != null}, groundPlane={_groundPlane != null}, mesh={_groundPlane?.Mesh != null}");
        }
        
        // Update collision body size
        UpdateCollisionBody(width, depth);
    }
    
    private void UpdateCollisionBody(float width, float depth)
    {
        var collisionBody = GetNodeOrNull<StaticBody3D>("GroundPlane/GroundCollision");
        if (collisionBody == null)
        {
            return;
        }
        
        var collisionShape = collisionBody.GetChild<CollisionShape3D>(0);
        if (collisionShape != null && collisionShape.Shape is BoxShape3D boxShape)
        {
            boxShape.Size = new Vector3(width, 0.1f, depth);
            GD.Print($"Garden: Updated collision body size to {width}m x {depth}m");
        }
    }
}

