#nullable enable
using Godot;

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
			GD.Print($"Player: Loaded settings - Sensitivity: {MouseSensitivity}, Speed: {Speed}");
		}
		
		GD.Print($"Player: First-person camera created");
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
					GD.Print("Player: Mouse capture released (ALT pressed)");
				}
				else if (Input.MouseMode == Input.MouseModeEnum.Visible)
				{
					Input.MouseMode = Input.MouseModeEnum.Captured;
					GD.Print("Player: Mouse capture enabled (ALT pressed)");
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
