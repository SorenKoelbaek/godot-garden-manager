using Godot;

public partial class VegetablePlot : Node3D
{
	private const float BaseSize = 1.0f; // Base size from scene (1x1)
	
	public override void _Ready()
	{
		// Nothing needed
	}
	
	public void SetSize(float width, float depth)
	{
		// width and depth are already in meters from WorldManager
		
		// Scale the root Node3D transform
		// X scale = width, Z scale = depth, Y scale = 1.0 (keep height constant)
		Scale = new Vector3(width / BaseSize, 1.0f, depth / BaseSize);
	}
}
