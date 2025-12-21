#nullable enable
using System.Threading.Tasks;
using GardenManager.Api;
using GardenManager.Models;
using Godot;
using Serilog;
using GardenManager.UI;

public partial class Player : CharacterBody3D
{
	// How fast the player moves in meters per second.
	[Export]
	public float Speed { get; set; } = 5.0f;
	
	// Running speed multiplier
	[Export]
	public float RunMultiplier { get; set; } = 2.0f;
	
	// Jump velocity in meters per second.
	[Export]
	public float JumpVelocity { get; set; } = 4.5f;
	
	// Crouch speed multiplier
	[Export]
	public float CrouchMultiplier { get; set; } = 0.5f;
	
	// The downward acceleration when in the air, in meters per second squared.
	[Export]
	public float FallAcceleration { get; set; } = 9.8f;
	
	// Crouch height reduction
	[Export]
	public float CrouchHeight { get; set; } = 0.5f;
	
	// Mouse sensitivity (can be changed by settings)
	public float MouseSensitivity { get; set; } = 0.003f;
	
	private float _normalHeight = 2.0f;
	private bool _isCrouching = false;
	private Camera3D _camera;
	private CollisionShape3D _collisionShape;
	
	// Camera rotation
	private float _cameraRotationX = 0.0f;
	
	// Raycast system
	private RaycastSystem? _raycastSystem;
	private InteractionOverlay? _interactionOverlay;
	private MessageHUD? _messageHUD;
	private Node3D? _currentHitObject;
	private Vector3 _currentHitPosition;
	private bool _isMouseButtonHeld = false;
	
	// Tool and API services
	private ToolManager? _toolManager;
	private PlotService? _plotService;
	
	[Signal]
	public delegate void PlotInteractedEventHandler(string plotUuid, Node3D hitObject);
	
	[Signal]
	public delegate void ToolInteractionEventHandler(int tool, string interactionType, Node3D? hitObject, Vector3 hitPosition);

	public override void _Ready()
	{
		_collisionShape = GetNode<CollisionShape3D>("CollisionShape3D");
		if (_collisionShape.Shape is CapsuleShape3D capsuleShape)
		{
			_normalHeight = capsuleShape.Height;
		}
		
		// Add to player group for easy finding
		AddToGroup("player");
		
		// Create camera attached to player head
		_camera = new Camera3D();
		_camera.Name = "Camera3D";
		_camera.Position = new Vector3(0, _normalHeight * 0.5f, 0);
		_camera.Current = true;
		AddChild(_camera);
		
		// Capture mouse
		Input.MouseMode = Input.MouseModeEnum.Captured;
		
		// Load settings
		var credentialManager = new GardenManager.Auth.CredentialManager();
		var settings = credentialManager.LoadSettings();
		if (settings != null)
		{
			MouseSensitivity = settings.MouseSensitivity;
			Speed = settings.PlayerSpeed;
			Log.Debug("Player: Loaded settings - Sensitivity: {Sensitivity}, Speed: {Speed}", MouseSensitivity, Speed);
		}
		
		// Initialize raycast system
		InitializeRaycastSystem();
		
		Log.Debug("Player: First-person camera created");
	}
	
	private void InitializeRaycastSystem()
	{
		// Create raycast system
		_raycastSystem = new RaycastSystem();
		_raycastSystem.Name = "RaycastSystem";
		AddChild(_raycastSystem);
		_raycastSystem.Initialize(_camera);
		
		// Connect signals
		_raycastSystem.HitObject += OnRaycastHit;
		_raycastSystem.NoHit += OnRaycastNoHit;
		
		// Get or create interaction overlay
		var mainWorld = GetTree().CurrentScene;
		_interactionOverlay = mainWorld?.GetNodeOrNull<InteractionOverlay>("InteractionOverlay");
		if (_interactionOverlay == null)
		{
			_interactionOverlay = new InteractionOverlay();
			_interactionOverlay.Name = "InteractionOverlay";
			mainWorld?.AddChild(_interactionOverlay);
		}
		
		// Get message HUD (now in HBoxContainer)
		var uiCanvas = mainWorld?.GetNodeOrNull<CanvasLayer>("UICanvas");
		var gameHUD = uiCanvas?.GetNodeOrNull<GameHUD>("GameHUD");
		_messageHUD = gameHUD?.GetNodeOrNull<MessageHUD>("HBoxContainer/MessageHUD");
		
		// Get ToolManager
		_toolManager = GetNodeOrNull<ToolManager>("/root/ToolManager");
		
		// Get API client and create PlotService
		var apiClient = GetNodeOrNull<ApiClient>("/root/ApiClient");
		if (apiClient != null)
		{
			_plotService = new PlotService(apiClient);
		}
		
		Log.Debug("Player: Raycast system initialized");
	}
	
	private void OnRaycastHit(Node3D? hitObject, float distance, Vector3 hitPosition, Vector3 hitNormal)
	{
		_currentHitObject = hitObject;
		_currentHitPosition = hitPosition;
		
		// Update overlay
		if (_interactionOverlay != null)
		{
			_interactionOverlay.ShowAt(hitPosition);
		}
		
		// Update message HUD
		if (_messageHUD != null)
		{
			_messageHUD.UpdateMessage(hitObject, distance);
		}
	}
	
	private void OnRaycastNoHit()
	{
		_currentHitObject = null;
		
		// Hide overlay
		if (_interactionOverlay != null)
		{
			_interactionOverlay.HideOverlay();
		}
		
		// Clear message HUD
		if (_messageHUD != null)
		{
			_messageHUD.ClearMessage();
		}
	}
	
	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseMotion mouseMotion && Input.MouseMode == Input.MouseModeEnum.Captured)
		{
			// Rotate player horizontally (Y axis)
			RotateY(-mouseMotion.Relative.X * MouseSensitivity);
			
			// Rotate camera vertically (X axis)
			_cameraRotationX -= mouseMotion.Relative.Y * MouseSensitivity;
			_cameraRotationX = Mathf.Clamp(_cameraRotationX, -Mathf.Pi / 2.0f + 0.1f, Mathf.Pi / 2.0f - 0.1f);
			_camera.Rotation = new Vector3(_cameraRotationX, 0, 0);
			
			// If mouse button is held, emit tool interaction for continuous painting
			if (_isMouseButtonHeld)
			{
				HandleMouseDrag();
			}
		}
		
		// Toggle mouse capture with ALT (only when menu is not open)
		if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.Alt)
		{
			var mainWorld = GetTree().CurrentScene;
			var mainMenu = mainWorld?.GetNodeOrNull<MainMenu>("UICanvas/MainMenu");
			
			// Only toggle if menu is not visible
			if (mainMenu == null || !mainMenu.Visible)
			{
				if (Input.MouseMode == Input.MouseModeEnum.Captured)
				{
					Input.MouseMode = Input.MouseModeEnum.Visible;
					Log.Debug("Player: Mouse capture released (ALT pressed)");
				}
				else if (Input.MouseMode == Input.MouseModeEnum.Visible)
				{
					Input.MouseMode = Input.MouseModeEnum.Captured;
					Log.Debug("Player: Mouse capture enabled (ALT pressed)");
				}
			}
		}
		
		// Open main menu with Escape
		if (@event is InputEventKey keyEvent2 && keyEvent2.Pressed && keyEvent2.Keycode == Key.Escape)
		{
			var mainWorld = GetTree().CurrentScene;
			var mainMenu = mainWorld?.GetNodeOrNull<MainMenu>("UICanvas/MainMenu");
			if (mainMenu != null && !mainMenu.Visible)
			{
				mainMenu.ShowMenu();
			}
		}
		
		// Handle E key press
		if (@event is InputEventKey keyEvent3 && keyEvent3.Pressed && keyEvent3.Keycode == Key.E)
		{
			HandleEKeyPress();
		}
		
		// Handle left mouse button for tool interactions
		if (@event is InputEventMouseButton mouseButton && mouseButton.ButtonIndex == MouseButton.Left)
		{
			_isMouseButtonHeld = mouseButton.Pressed;
			if (mouseButton.Pressed)
			{
				HandleMouseClick();
			}
		}
	}
	
	private async void HandleEKeyPress()
	{
		// Don't interact if main menu is open
		var mainWorld = GetTree().CurrentScene;
		var mainMenu = mainWorld?.GetNodeOrNull<MainMenu>("UICanvas/MainMenu");
		if (mainMenu != null && mainMenu.Visible)
		{
			return;
		}
		
		if (_toolManager == null)
		{
			return;
		}
		
		// Only interact with plots in Hands mode
		if (_toolManager.CurrentTool == ToolType.Hands)
		{
			// Check if we have a hit object
			if (_currentHitObject != null)
			{
				// Try to get plot UUID from hit object
				string? plotUuid = GetPlotUuidFromObject(_currentHitObject);
				
				if (!string.IsNullOrEmpty(plotUuid))
				{
					Log.Debug("Player: E key pressed on plot {PlotUuid} in Hands mode", plotUuid);
					
					// Emit event for other systems to hook into
					EmitSignal(SignalName.PlotInteracted, plotUuid, _currentHitObject);
					Log.Debug("Player: PlotInteracted event emitted for plot {PlotUuid}", plotUuid);
					
					// Fetch and log crop information
					if (_plotService != null)
					{
						var plotDetails = await _plotService.GetPlotDetailsAsync(plotUuid);
						if (plotDetails?.CurrentPlanted != null && plotDetails.CurrentPlanted.Count > 0)
						{
							Log.Debug("Player: Crop information for plot {PlotUuid}:", plotUuid);
							foreach (var planted in plotDetails.CurrentPlanted)
							{
								Log.Debug("  - Crop: {CropName}, Date Planted: {DatePlanted}, Amount: {Amount}, Method: {Method}, Orientation: {Orientation}",
									planted.Crop?.Name ?? "Unknown",
									planted.DatePlanted,
									planted.Amount?.ToString() ?? "N/A",
									planted.Method,
									planted.Orientation ?? "N/A");
							}
						}
						else
						{
							Log.Debug("Player: No crops currently planted in plot {PlotUuid}", plotUuid);
						}
					}
					else
					{
						Log.Warning("Player: PlotService is null, cannot fetch crop information");
					}
				}
				else
				{
					Log.Debug("Player: E key pressed but no plot UUID found");
				}
			}
		}
		else
		{
			// Other tools (Mower, Hoe, Plant) - emit tool interaction event
			EmitToolInteraction("E");
		}
	}
	
	private void HandleMouseClick()
	{
		// Don't interact if main menu is open
		var mainWorld = GetTree().CurrentScene;
		var mainMenu = mainWorld?.GetNodeOrNull<MainMenu>("UICanvas/MainMenu");
		if (mainMenu != null && mainMenu.Visible)
		{
			return;
		}
		
		// Only handle mouse clicks when mouse is captured (not in menu)
		if (Input.MouseMode != Input.MouseModeEnum.Captured)
		{
			return;
		}
		
		if (_toolManager == null)
		{
			return;
		}
		
		// Only emit tool interaction events when not in Hands mode
		if (_toolManager.CurrentTool != ToolType.Hands)
		{
			EmitToolInteraction("MouseClick");
		}
	}
	
	private void HandleMouseDrag()
	{
		// Don't interact if main menu is open
		var mainWorld = GetTree().CurrentScene;
		var mainMenu = mainWorld?.GetNodeOrNull<MainMenu>("UICanvas/MainMenu");
		if (mainMenu != null && mainMenu.Visible)
		{
			return;
		}
		
		// Only handle when mouse is captured (not in menu)
		if (Input.MouseMode != Input.MouseModeEnum.Captured)
		{
			return;
		}
		
		if (_toolManager == null)
		{
			return;
		}
		
		// Only emit tool interaction events when not in Hands mode and we have a hit object
		if (_toolManager.CurrentTool != ToolType.Hands && _currentHitObject != null)
		{
			EmitToolInteraction("MouseDrag");
		}
	}
	
	private void EmitToolInteraction(string interactionType)
	{
		if (_toolManager == null)
		{
			return;
		}
		
		ToolType currentTool = _toolManager.CurrentTool;
		
		// Check what we hit
		string? hitObjectType = GetHitObjectType(_currentHitObject);
		
		// Only emit if we have a valid hit
		if (_currentHitObject != null && !string.IsNullOrEmpty(hitObjectType))
		{
			Log.Debug("Player: Tool interaction - Tool: {Tool}, Type: {InteractionType}, Hit: {HitObjectType}", 
				currentTool, interactionType, hitObjectType);
			
			EmitSignal(SignalName.ToolInteraction, (int)currentTool, interactionType, _currentHitObject, _currentHitPosition);
		}
	}
	
	private string? GetHitObjectType(Node3D? hitObject)
	{
		if (hitObject == null)
		{
			return null;
		}
		
		string nodeName = hitObject.Name.ToString();
		
		// Check for ground collision
		if (nodeName.Contains("GroundCollision") || nodeName.Contains("GroundPlane"))
		{
			return "GroundCollision";
		}
		
		// Check for plot
		if (GetPlotUuidFromObject(hitObject) != null)
		{
			return "Plot";
		}
		
		// Return node name as fallback
		return nodeName;
	}
	
	private string? GetPlotUuidFromObject(Node3D node)
	{
		// Traverse up the tree to find VegetablePlot
		Node3D? current = node;
		int maxDepth = 10; // Prevent infinite loops
		int depth = 0;
		
		while (current != null && depth < maxDepth)
		{
			// Check if current node is a VegetablePlot
			if (current is VegetablePlot vegetablePlot && !string.IsNullOrEmpty(vegetablePlot.PlotUuid))
			{
				return vegetablePlot.PlotUuid;
			}
			
			// Move up the tree
			var parent = current.GetParent();
			current = parent as Node3D;
			depth++;
		}
		
		return null;
	}

	public override void _PhysicsProcess(double delta)
	{
		var inputDirection = Vector2.Zero;

		// Get input direction
		if (Input.IsActionPressed("move_right"))
		{
			inputDirection.X += 1.0f;
		}
		if (Input.IsActionPressed("move_left"))
		{
			inputDirection.X -= 1.0f;
		}
		if (Input.IsActionPressed("move_back"))
		{
			inputDirection.Y += 1.0f;
		}
		if (Input.IsActionPressed("move_forward"))
		{
			inputDirection.Y -= 1.0f;
		}

		// Calculate movement direction relative to player's forward direction
		var direction = Vector3.Zero;
		if (inputDirection != Vector2.Zero)
		{
			inputDirection = inputDirection.Normalized();
			
			// Get player's forward and right vectors (from player's rotation)
			var forward = -GlobalTransform.Basis.Z; // Forward is -Z in Godot
			var right = GlobalTransform.Basis.X;
			
			// Remove vertical component for ground movement
			forward.Y = 0;
			right.Y = 0;
			forward = forward.Normalized();
			right = right.Normalized();
			
			// Combine forward/back and left/right movement
			direction = (forward * -inputDirection.Y) + (right * inputDirection.X);
			direction = direction.Normalized();
		}

		// Handle crouch - crouch height is half of normal height
		bool wantsToCrouch = Input.IsActionPressed("crouch");
		if (wantsToCrouch && !_isCrouching && IsOnFloor())
		{
			_isCrouching = true;
			if (_collisionShape.Shape is CapsuleShape3D shape)
			{
				// Crouch to half height (1.0m if normal is 2.0m)
				shape.Height = _normalHeight * 0.5f;
				// Position camera at half of crouch height (0.5m from bottom)
				_camera.Position = new Vector3(0, shape.Height * 0.5f, 0);
			}
		}
		else if (!wantsToCrouch && _isCrouching)
		{
			_isCrouching = false;
			if (_collisionShape.Shape is CapsuleShape3D shape)
			{
				shape.Height = _normalHeight;
				// Position camera at half of normal height
				_camera.Position = new Vector3(0, shape.Height * 0.5f, 0);
			}
		}

		// Calculate current speed (walk/run/crouch)
		float currentSpeed = Speed;
		if (Input.IsActionPressed("run") && !_isCrouching)
		{
			currentSpeed *= RunMultiplier;
		}
		else if (_isCrouching)
		{
			currentSpeed *= CrouchMultiplier;
		}

		// Ground velocity
		var velocity = Velocity;
		velocity.X = direction.X * currentSpeed;
		velocity.Z = direction.Z * currentSpeed;

		// Handle jump
		if (Input.IsActionJustPressed("jump") && IsOnFloor() && !_isCrouching)
		{
			velocity.Y = JumpVelocity;
		}

		// Vertical velocity (gravity)
		if (!IsOnFloor())
		{
			velocity.Y -= FallAcceleration * (float)delta;
		}

		// Move the character
		Velocity = velocity;
		MoveAndSlide();
	}
}
