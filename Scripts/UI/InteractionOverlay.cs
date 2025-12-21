#nullable enable
using Godot;
using Serilog;

public partial class InteractionOverlay : Node3D
{
	private MeshInstance3D? _discMesh;
	private Sprite3D? _iconSprite;
	private ToolManager? _toolManager;
	private float _currentDiscDiameter = 0.1f; // Current disc diameter (will be updated from ToolManager)
	private const float DiscThickness = 0.01f; // 1cm thick disc
	private const float IconSizeRatio = 0.6f; // Icon size as ratio of disc diameter (60% of disc)

	public override void _Ready()
	{
		Log.Debug("InteractionOverlay: _Ready() called");
		CreateDisc();
		CreateIconSprite();
		SubscribeToToolManager();
		Visible = false;
	}

	private void CreateDisc()
	{
		UpdateDiscMesh(_currentDiscDiameter);
	}
	
	private void UpdateDiscMesh(float diameter)
	{
		_currentDiscDiameter = diameter;
		
		// Remove old disc if it exists
		if (_discMesh != null)
		{
			_discMesh.QueueFree();
		}
		
		// Create a flat circle using ArrayMesh
		var arrays = new Godot.Collections.Array();
		arrays.Resize((int)Mesh.ArrayType.Max);
		
		var vertices = new Vector3[32];
		var indices = new int[96]; // 32 triangles * 3 indices
		float radius = diameter / 2.0f;
		
		// Create vertices in a circle (flat on XZ plane, Y = 0)
		for (int i = 0; i < 32; i++)
		{
			float angle = (i / 32.0f) * Mathf.Pi * 2.0f;
			vertices[i] = new Vector3(
				Mathf.Cos(angle) * radius,
				0.0f,
				Mathf.Sin(angle) * radius
			);
		}
		
		// Create triangles (fan from center)
		// Center is at index 0, circle vertices start at 1
		int index = 0;
		for (int i = 0; i < 32; i++)
		{
			indices[index++] = 0; // Center vertex
			indices[index++] = i + 1;
			indices[index++] = ((i + 1) % 32) + 1; // Next vertex, wrapping around
		}
		
		// Add center vertex at the start
		var allVertices = new Vector3[33];
		allVertices[0] = Vector3.Zero; // Center
		for (int i = 0; i < 32; i++)
		{
			allVertices[i + 1] = vertices[i];
		}
		
		arrays[(int)Mesh.ArrayType.Vertex] = allVertices;
		arrays[(int)Mesh.ArrayType.Index] = indices;
		
		var arrayMesh = new ArrayMesh();
		arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
		
		_discMesh = new MeshInstance3D();
		_discMesh.Mesh = arrayMesh;

		// Create transparent white material
		var material = new StandardMaterial3D();
		material.AlbedoColor = new Color(1.0f, 1.0f, 1.0f, 0.15f); // White, 15% opacity - very transparent
		material.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
		material.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
		material.NoDepthTest = true; // Draw on top
		material.CullMode = BaseMaterial3D.CullModeEnum.Disabled; // Show from both sides
		material.BillboardMode = BaseMaterial3D.BillboardModeEnum.Disabled;

		_discMesh.MaterialOverride = material;

		// Mesh is already flat on XZ plane, no rotation needed
		AddChild(_discMesh);

		Log.Debug("InteractionOverlay: Disc created with diameter {Diameter}m", diameter);
	}

	/// <summary>
	/// Create icon sprite for displaying tool icons
	/// </summary>
	private void CreateIconSprite()
	{
		_iconSprite = new Sprite3D();
		_iconSprite.Billboard = BaseMaterial3D.BillboardModeEnum.Disabled;
		// PixelSize will be calculated dynamically when texture is set
		// Position icon at center of disc, slightly above ground (same Y as disc center)
		// Rotate to lay flat on XZ plane (like the disc)
		_iconSprite.Position = new Vector3(0, 0.001f, 0);
		_iconSprite.Rotation = new Vector3(-Mathf.Pi / 2.0f, 0, 0); // Rotate -90 degrees on X axis to lay flat
		_iconSprite.Visible = false;
		_iconSprite.NoDepthTest = true; // Render on top like the disc
		// Icon is a child of InteractionOverlay, so it will be positioned on the ground with the disc
		AddChild(_iconSprite);
		
		Log.Debug("InteractionOverlay: Icon sprite created at local position {Position}", _iconSprite.Position);
	}


	/// <summary>
	/// Subscribe to ToolManager for tool changes
	/// </summary>
	private void SubscribeToToolManager()
	{
		_toolManager = GetNodeOrNull<ToolManager>("/root/ToolManager");
		if (_toolManager != null)
		{
			_toolManager.ToolChangedDetailed += OnToolChangedDetailed;
			// Update for current tool
			if (_toolManager.GetToolInfo(_toolManager.CurrentTool) is { } toolInfo)
			{
				OnToolChangedDetailed(_toolManager.CurrentTool, toolInfo.IconPath, toolInfo.DiscDiameter);
			}
		}
		else
		{
			Log.Warning("InteractionOverlay: ToolManager not found");
		}
	}

	/// <summary>
	/// Calculate PixelSize based on texture dimensions and disc diameter to achieve proportional world size
	/// </summary>
	private void UpdateIconPixelSize(Texture2D? texture, float discDiameter)
	{
		if (_iconSprite == null || texture == null)
		{
			return;
		}

		// Get texture size
		int textureWidth = texture.GetWidth();
		if (textureWidth > 0)
		{
			// Calculate icon world size as a percentage of disc diameter
			float iconWorldSize = discDiameter * IconSizeRatio;
			// Calculate PixelSize so that the icon's world size matches the calculated size
			// PixelSize = desired world size / texture width
			_iconSprite.PixelSize = iconWorldSize / textureWidth;
			Log.Debug("InteractionOverlay: Updated icon PixelSize to {PixelSize} for texture size {TextureSize}, disc diameter {DiscDiameter}m, icon size {IconSize}m", 
				_iconSprite.PixelSize, textureWidth, discDiameter, iconWorldSize);
		}
	}

	/// <summary>
	/// Handle detailed tool change event with icon path and disc diameter
	/// </summary>
	private void OnToolChangedDetailed(ToolType tool, string iconPath, float discDiameter)
	{
		Log.Debug("InteractionOverlay: Tool changed to {Tool}, icon: {IconPath}, diameter: {Diameter}m", tool, iconPath, discDiameter);
		
		// Update disc diameter
		UpdateDiscMesh(discDiameter);
		
		// Update icon
		if (_iconSprite == null)
		{
			return;
		}
		
		if (string.IsNullOrEmpty(iconPath))
		{
			// No icon for this tool (e.g., Hands)
			_iconSprite.Visible = false;
		}
		else
		{
			// Load and set icon
			var iconTexture = GD.Load<Texture2D>(iconPath);
			if (iconTexture != null)
			{
				_iconSprite.Texture = iconTexture;
				UpdateIconPixelSize(iconTexture, discDiameter);
				_iconSprite.Visible = true;
			}
			else
			{
				Log.Warning("InteractionOverlay: Failed to load icon from path: {IconPath}", iconPath);
				_iconSprite.Visible = false;
			}
		}
	}

	/// <summary>
	/// Show the overlay at the specified position
	/// </summary>
	public void ShowAt(Vector3 position)
	{
		// Ensure Y position is above ground
		if (position.Y <= 0)
		{
			position = new Vector3(position.X, 0.1f, position.Z);
		}

		GlobalPosition = position;
		Visible = true;
	}

	/// <summary>
	/// Hide the overlay
	/// </summary>
	public void HideOverlay()
	{
		Visible = false;
	}

	public override void _ExitTree()
	{
		if (_toolManager != null)
		{
			_toolManager.ToolChangedDetailed -= OnToolChangedDetailed;
		}
	}
}

