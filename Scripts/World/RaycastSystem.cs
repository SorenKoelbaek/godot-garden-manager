#nullable enable
using Godot;
using Serilog;

public partial class RaycastSystem : Node
{
	[Export]
	public float RaycastLength { get; set; } = 2.0f; // 2 meters default

	[Signal]
	public delegate void HitObjectEventHandler(Node3D? hitObject, float distance, Vector3 hitPosition, Vector3 hitNormal);

	[Signal]
	public delegate void NoHitEventHandler();

	private Camera3D? _camera;
	private PhysicsDirectSpaceState3D? _spaceState;

	public override void _Ready()
	{
		Log.Debug("RaycastSystem: _Ready() called");
	}

	/// <summary>
	/// Initialize the raycast system with a camera
	/// </summary>
	public void Initialize(Camera3D camera)
	{
		_camera = camera;
		_spaceState = camera.GetWorld3D().DirectSpaceState;
		Log.Debug("RaycastSystem: Initialized with camera");
	}

	public override void _Process(double delta)
	{
		if (_camera == null || _spaceState == null)
		{
			return;
		}

		PerformRaycast();
	}

	private void PerformRaycast()
	{
		if (_camera == null || _spaceState == null)
		{
			return;
		}

		// Get camera position and forward direction
		Vector3 from = _camera.GlobalPosition;
		Vector3 forward = -_camera.GlobalTransform.Basis.Z; // Forward is -Z in Godot
		Vector3 to = from + forward * RaycastLength;

		// Create raycast query
		var query = PhysicsRayQueryParameters3D.Create(from, to);
		query.Exclude = new Godot.Collections.Array<Rid>(); // Exclude nothing for now

		// Perform raycast
		var result = _spaceState.IntersectRay(query);

		if (result.Count > 0)
		{
			// Hit something
			var hitObject = result["collider"].AsGodotObject() as Node3D;
			var hitPosition = result["position"].AsVector3();
			var hitNormal = result["normal"].AsVector3();
			float distance = from.DistanceTo(hitPosition);

			// Ensure Y position is above ground
			if (hitPosition.Y <= 0)
			{
				hitPosition = new Vector3(hitPosition.X, 0.1f, hitPosition.Z);
			}

			EmitSignal(SignalName.HitObject, hitObject, distance, hitPosition, hitNormal);
		}
		else
		{
			// No hit
			EmitSignal(SignalName.NoHit);
		}
	}

	/// <summary>
	/// Get the current hit information (for external queries)
	/// </summary>
	public (Node3D? hitObject, float distance, Vector3 hitPosition) GetCurrentHit()
	{
		if (_camera == null || _spaceState == null)
		{
			return (null, 0, Vector3.Zero);
		}

		Vector3 from = _camera.GlobalPosition;
		Vector3 forward = -_camera.GlobalTransform.Basis.Z;
		Vector3 to = from + forward * RaycastLength;

		var query = PhysicsRayQueryParameters3D.Create(from, to);
		var result = _spaceState.IntersectRay(query);

		if (result.Count > 0)
		{
			var hitObject = result["collider"].AsGodotObject() as Node3D;
			var hitPosition = result["position"].AsVector3();
			float distance = from.DistanceTo(hitPosition);

			// Ensure Y position is above ground
			if (hitPosition.Y <= 0)
			{
				hitPosition = new Vector3(hitPosition.X, 0.1f, hitPosition.Z);
			}

			return (hitObject, distance, hitPosition);
		}

		return (null, 0, Vector3.Zero);
	}
}

