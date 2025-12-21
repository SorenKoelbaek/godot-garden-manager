using Godot;
using Serilog;

public partial class BrushOverlay : Node3D
{
    [Export]
    public float BrushSize { get; set; } = 1.0f;

    [Export]
    public Color BrushColor { get; set; } = new Color(1, 1, 1, 0.3f);

    private MeshInstance3D _brushMesh;
    private Camera3D _camera;
    private ToolManager _toolManager;
    private Garden _garden;
    private Player? _player;

    private ImageTexture _growthTexture;   // Runtime texture from Garden
    private Image _growthImage;            // Runtime image buffer

    private bool _isPainting = false;

    // ------------------------------------------------------------------------
    public override void _Ready()
    {
        Log.Debug("BrushOverlay: Initializing");

        _toolManager = GetNode<ToolManager>("/root/ToolManager");

        // DO NOT LOOK FOR THE CAMERA HERE — Player creates the camera in _Ready()

        // ---- FIND GARDEN ----
        var mainWorld = GetTree().CurrentScene;

        if (mainWorld != null)
        {
            _garden = mainWorld.GetNodeOrNull<Garden>("Garden")
                      ?? FindGardenRecursive(mainWorld);
        }

        if (_garden == null)
            Log.Debug("BrushOverlay: Garden not found yet, will retry later");


        if (_toolManager != null)
            _toolManager.ToolChanged += OnToolChanged;

        Visible = false;

        // Try initial texture load
        if (_garden != null)
            LoadGrowthTextureFromGarden();
        
        // Find and connect to Player for tool interaction events
        FindAndConnectPlayer();
    }
    
    private void FindAndConnectPlayer()
    {
        var mainWorld = GetTree().CurrentScene;
        if (mainWorld != null)
        {
            _player = mainWorld.GetNodeOrNull<Player>("Player");
            if (_player != null)
            {
                _player.ToolInteraction += OnToolInteraction;
                Log.Debug("BrushOverlay: Connected to Player for tool interaction events");
            }
            else
            {
                Log.Debug("BrushOverlay: Player not found, will retry later");
            }
        }
    }
    
    private void OnToolInteraction(int toolInt, string interactionType, Node3D? hitObject, Vector3 hitPosition)
    {
        ToolType tool = (ToolType)toolInt;
        
        // Only handle ground collision interactions for mower tool
        if (tool != ToolType.Mower)
            return;
        
        // Check if hit object is ground collision
        if (hitObject == null)
            return;
        
        string nodeName = hitObject.Name.ToString();
        if (!nodeName.Contains("GroundCollision") && !nodeName.Contains("GroundPlane"))
            return;
        
        // Handle both mouse click and mouse drag interactions
        if (interactionType != "MouseClick" && interactionType != "MouseDrag")
            return;
        
        // Paint at the hit position
        PaintAtPosition(hitPosition);
    }

    // ------------------------------------------------------------------------
    // Recursive camera search (works even after Player spawns camera dynamically)
    private Camera3D FindCameraRecursive(Node node)
    {
        if (node is Camera3D cam)
            return cam;

        foreach (var child in node.GetChildren())
        {
            var result = FindCameraRecursive(child);
            if (result != null)
                return result;
        }

        return null;
    }

    // ------------------------------------------------------------------------
    private void LoadGrowthTextureFromGarden()
    {
        if (_garden == null)
            return;

        _growthTexture = _garden.GetGrowthTexture();

        if (_growthTexture == null)
        {
            Log.Error("BrushOverlay: Garden returned null growth texture!");
            return;
        }

        // Get the image directly from Garden (it maintains the image buffer)
        _growthImage = _garden.GetGrowthImage();

        if (_growthImage == null)
        {
            Log.Error("BrushOverlay: Garden returned null growth image!");
            return;
        }

        Log.Debug("BrushOverlay: Loaded runtime growth texture Size={Width}x{Height}", _growthImage.GetWidth(), _growthImage.GetHeight());
    }

    // ------------------------------------------------------------------------
    private void OnToolChanged(ToolType tool)
    {
        Visible = (tool == ToolType.Mower);
        Log.Debug("BrushOverlay: Visibility set to {Visible}", Visible);
    }

    // ------------------------------------------------------------------------
    public override void _Process(double delta)
    {
        // --------------------------------------------------------
        // 1. Late Camera Detection (Player creates it in _Ready)
        // --------------------------------------------------------
        if (_camera == null)
        {
            var scene = GetTree().CurrentScene;
            if (scene != null)
            {
                _camera = FindCameraRecursive(scene);
                if (_camera != null)
                {
                    Log.Debug("BrushOverlay: Camera attached late.");
                }
            }
        }

        // --------------------------------------------------------
        // 2. Late Garden Detection
        // --------------------------------------------------------
        if (_garden == null)
        {
            var mainWorld = GetTree().CurrentScene;
            if (mainWorld != null)
            {
                _garden = mainWorld.GetNodeOrNull<Garden>("Garden")
                          ?? FindGardenRecursive(mainWorld);
            }
        }
        
        // --------------------------------------------------------
        // 2.5. Late Player Detection
        // --------------------------------------------------------
        if (_player == null)
        {
            var mainWorld = GetTree().CurrentScene;
            if (mainWorld != null)
            {
                _player = mainWorld.GetNodeOrNull<Player>("Player");
                if (_player != null)
                {
                    _player.ToolInteraction += OnToolInteraction;
                    Log.Debug("BrushOverlay: Connected to Player for tool interaction events (late)");
                }
            }
        }

        // --------------------------------------------------------
        // 3. Load growth texture once garden exists
        // --------------------------------------------------------
        if (_garden != null && _growthTexture == null)
        {
            LoadGrowthTextureFromGarden();
        }

        // Camera or garden still missing → exit until ready
        if (!Visible || _camera == null || _garden == null)
            return;

        UpdateBrushPosition();
    }

    // ------------------------------------------------------------------------
    private Garden FindGardenRecursive(Node node)
    {
        if (node is Garden g)
            return g;

        foreach (var child in node.GetChildren())
        {
            var found = FindGardenRecursive(child);
            if (found != null)
                return found;
        }

        return null;
    }


    // ------------------------------------------------------------------------
    private void UpdateBrushPosition()
    {
        if (_camera == null)
            return;

        var space = GetWorld3D().DirectSpaceState;

        var from = _camera.GlobalPosition;
        var forward = -_camera.GlobalTransform.Basis.Z;
        var to = from + forward * 100f;

        var query = PhysicsRayQueryParameters3D.Create(from, to);
        query.CollisionMask = 1; // Ground

        var result = space.IntersectRay(query);

        if (result.Count > 0)
        {
            var pos = (Vector3)result["position"];
            var normal = (Vector3)result["normal"];

            // Position brush slightly above ground
            GlobalPosition = pos + normal * 0.02f;

            // Force brush to stay flat on ground, no rotation allowed
            GlobalRotation = new Vector3(0, GlobalRotation.Y, 0);
        }
    }

    // ------------------------------------------------------------------------
    private void PaintAtPosition(Vector3 worldPos)
    {
        if (_growthImage == null || _garden == null || _toolManager == null)
            return;

        var localPos = _garden.GlobalTransform.AffineInverse() * worldPos;

        float width = _garden.Width;
        float depth = _garden.Depth;

        float u = (localPos.X + width / 2f) / width;
        float v = (localPos.Z + depth / 2f) / depth;

        u = Mathf.Clamp(u, 0f, 1f);
        v = Mathf.Clamp(v, 0f, 1f);

        int imgW = _growthImage.GetWidth();
        int imgH = _growthImage.GetHeight();

        int centerX = (int)(u * imgW);
        int centerY = (int)(v * imgH);

        // Get brush size from tool's disc diameter
        var toolInfo = _toolManager.GetToolInfo(_toolManager.CurrentTool);
        float brushDiameter = toolInfo?.DiscDiameter ?? BrushSize;
        
        // Calculate radius in pixels based on tool diameter
        int radius = (int)(brushDiameter * imgW / width);

        for (int y = -radius; y <= radius; y++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                if (x * x + y * y > radius * radius)
                    continue;

                int px = centerX + x;
                int py = centerY + y;

                if (px >= 0 && px < imgW && py >= 0 && py < imgH)
                    _growthImage.SetPixel(px, py, Colors.White);
            }
        }

        // Update texture through Garden (it maintains the image buffer)
        if (_garden != null)
        {
            _garden.UpdateGrowthTexture();
        }

        Log.Debug("BrushOverlay: Painted at pixel ({CenterX},{CenterY}) with diameter {Diameter}m (radius {Radius}px)", 
            centerX, centerY, brushDiameter, radius);
    }

    // ------------------------------------------------------------------------
    public override void _ExitTree()
    {
        if (_toolManager != null)
            _toolManager.ToolChanged -= OnToolChanged;
        
        if (_player != null)
            _player.ToolInteraction -= OnToolInteraction;
    }
}
