#nullable enable
using Godot;
using Serilog;

public partial class MessageHUD : Label
{
	public override void _Ready()
	{
		Log.Debug("MessageHUD: _Ready() called");
		
		// This is now a Label directly in the HBoxContainer
		HorizontalAlignment = HorizontalAlignment.Right;
		VerticalAlignment = VerticalAlignment.Center;
		AutowrapMode = TextServer.AutowrapMode.Off;
		Text = "";
		
		Log.Debug("MessageHUD: Initialized as Label in HBoxContainer");
	}

	/// <summary>
	/// Update the message with hit object information
	/// </summary>
	public void UpdateMessage(Node3D? hitObject, float distance)
	{
		if (hitObject != null)
		{
			// Get object name (try to get plot UUID or use node name)
			string objectName = GetObjectName(hitObject);
			Text = $"{objectName}\n{distance:F2}m";
			Visible = true;
		}
		else
		{
			Text = "";
			Visible = false;
		}
	}

	/// <summary>
	/// Clear the message
	/// </summary>
	public void ClearMessage()
	{
		Text = "";
		Visible = false;
	}

	private string GetObjectName(Node3D node)
	{
		// Traverse up the tree to find VegetablePlot
		Node3D? current = node;
		int maxDepth = 20; // Increased depth to find parent plots
		int depth = 0;
		
		// First, check the node itself
		if (current is VegetablePlot vegetablePlotSelf)
		{
			if (!string.IsNullOrEmpty(vegetablePlotSelf.PlotUuid))
			{
				return vegetablePlotSelf.PlotUuid;
			}
			if (!string.IsNullOrEmpty(vegetablePlotSelf.PlotName))
			{
				return vegetablePlotSelf.PlotName;
			}
		}
		
		// Then traverse up the tree
		while (current != null && depth < maxDepth)
		{
			// Check if current node is a VegetablePlot
			if (current is VegetablePlot vegetablePlot)
			{
				// Prefer PlotUuid, fallback to PlotName
				if (!string.IsNullOrEmpty(vegetablePlot.PlotUuid))
				{
					return vegetablePlot.PlotUuid;
				}
				if (!string.IsNullOrEmpty(vegetablePlot.PlotName))
				{
					return vegetablePlot.PlotName;
				}
				// Even if UUID/Name are empty, return the node name to indicate it's a plot
				return $"Plot: {current.Name}";
			}
			
			// Move up the tree
			var parent = current.GetParent();
			current = parent as Node3D;
			depth++;
		}

		// Check if node name contains plot info
		string nodeName = node.Name.ToString();
		if (nodeName.Contains("Plot") || nodeName.Contains("Tree"))
		{
			return nodeName;
		}

		// Default to node name (this is probably a child node like "F" or collision shape)
		return nodeName;
	}
}

