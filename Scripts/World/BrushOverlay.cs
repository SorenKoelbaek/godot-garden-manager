using Godot;

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

    private ImageTexture _growthTexture;   // Runtime texture from Garden
    private Image _growthImage;            // Runtime image buffer

    private bool _isPainting = false;

    // ------------------------------------------------------------------------
    public override void _Ready()
    {
        GD.Print("BrushOverlay: Initializing");

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
            GD.Print("BrushOverlay: Garden not found yet, will retry later");


        if (_toolManager != null)
            _toolManager.ToolChanged += OnToolChanged;

        Visible = false;

        // Try initial texture load
        if (_garden != null)
            LoadGrowthTextureFromGarden();
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
            GD.PrintErr("BrushOverlay: Garden returned null growth texture!");
            return;
        }

        _growthImage = _growthTexture.GetImage();

        if (_growthImage == null)
        {
            GD.PrintErr("BrushOverlay: growthTexture.GetImage() returned null!");
            return;
        }

        GD.Print($"BrushOverlay: Loaded runtime growth texture Size={_growthImage.GetWidth()}x{_growthImage.GetHeight()}");
    }

    // ------------------------------------------------------------------------
    private void OnToolChanged(ToolType tool)
    {
        Visible = (tool == ToolType.Mower);
        GD.Print($"BrushOverlay: Visibility set to {Visible}");
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
                    GD.Print("BrushOverlay: Camera attached late.");
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
    public override void _Input(InputEvent @event)
    {
        if (!Visible || _growthImage == null || _garden == null)
            return;

        if (_toolManager == null || _toolManager.CurrentTool != ToolType.Mower)
            return;

        if (@event is InputEventMouseButton mouse)
        {
            if (mouse.ButtonIndex == MouseButton.Left)
            {
                _isPainting = mouse.Pressed;

                if (mouse.Pressed)
                    PaintAtPosition(GlobalPosition);
            }
        }

        if (_isPainting && @event is InputEventMouseMotion)
            PaintAtPosition(GlobalPosition);
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
        if (_growthImage == null)
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

        int radius = (int)(BrushSize * imgW / width);

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

        _growthTexture.Update(_growthImage);

        GD.Print($"BrushOverlay: Painted at pixel ({centerX},{centerY})");
    }

    // ------------------------------------------------------------------------
    public override void _ExitTree()
    {
        if (_toolManager != null)
            _toolManager.ToolChanged -= OnToolChanged;
    }
}
