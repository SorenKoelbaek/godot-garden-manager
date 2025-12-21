using Godot;
using Serilog;

namespace GardenManager.UI
{
	/// <summary>
	/// Base class for reusable menu interfaces.
	/// Provides common functionality for modal overlays including input capture,
	/// visibility management, and mouse mode switching.
	/// Matches MainMenu visual style (centered VBoxContainer with buttons).
	/// </summary>
	public partial class BaseMenu : Control
	{
		protected bool _isOpen = false;

		public override void _Ready()
		{
			Log.Debug("BaseMenu: _Ready() called");
			Visible = false;
		}

		/// <summary>
		/// Show the menu and capture input
		/// </summary>
		public virtual void ShowMenu()
		{
			Visible = true;
			_isOpen = true;
			Input.MouseMode = Input.MouseModeEnum.Visible;
			Log.Debug("BaseMenu: Menu shown");
		}

		/// <summary>
		/// Hide the menu and release input
		/// </summary>
		public virtual void HideMenu()
		{
			Visible = false;
			_isOpen = false;
			Input.MouseMode = Input.MouseModeEnum.Captured;
			Log.Debug("BaseMenu: Menu hidden");
		}

		/// <summary>
		/// Toggle menu visibility
		/// </summary>
		public virtual void ToggleMenu()
		{
			if (_isOpen)
			{
				HideMenu();
			}
			else
			{
				ShowMenu();
			}
		}

		public bool IsOpen => _isOpen;
	}
}

