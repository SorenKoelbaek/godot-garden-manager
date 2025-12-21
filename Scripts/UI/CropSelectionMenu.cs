#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GardenManager.Api;
using GardenManager.Models;
using Godot;
using Serilog;

namespace GardenManager.UI
{
	public partial class CropSelectionMenu : BaseMenu
	{
		private VBoxContainer? _container;
		private ScrollContainer? _scrollContainer;
		private VBoxContainer? _cropsContainer;
		private Label? _titleLabel;
		private Button? _backButton;
		private PlotService? _plotService;
		private TimeManager? _timeManager;
		private string? _plotUuid;
		private string? _gardenUuid;
		private string? _plotGroupUuid;

		public override void _Ready()
		{
			Log.Debug("CropSelectionMenu: _Ready() called");

			// Get API client and create plot service
			var apiClient = GetNode<ApiClient>("/root/ApiClient");
			_plotService = new PlotService(apiClient);

			// Get TimeManager for current year
			_timeManager = GetNodeOrNull<TimeManager>("/root/TimeManager");

			// Create UI structure
			AnchorsPreset = 15;
			Visible = false;

			_container = new VBoxContainer();
			_container.AnchorsPreset = 8; // Center preset
			_container.AnchorLeft = 0.5f;
			_container.AnchorTop = 0.5f;
			_container.AnchorRight = 0.5f;
			_container.AnchorBottom = 0.5f;
			_container.OffsetLeft = -200;
			_container.OffsetTop = -250;
			_container.OffsetRight = 200;
			_container.OffsetBottom = 250;
			AddChild(_container);

			// Back button at the top
			_backButton = new Button();
			_backButton.Text = "Back";
			_backButton.Pressed += OnBackPressed;
			_container.AddChild(_backButton);

			var separator0 = new HSeparator();
			_container.AddChild(separator0);

			_titleLabel = new Label();
			_titleLabel.Text = "Select Crop to Plant";
			_titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
			_container.AddChild(_titleLabel);

			var separator = new HSeparator();
			_container.AddChild(separator);

			_scrollContainer = new ScrollContainer();
			_scrollContainer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
			_container.AddChild(_scrollContainer);

			_cropsContainer = new VBoxContainer();
			_cropsContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			_scrollContainer.AddChild(_cropsContainer);

			Log.Debug("CropSelectionMenu: UI structure created");
		}

		/// <summary>
		/// Open menu for a specific plot
		/// </summary>
		public async void OpenForPlot(string plotUuid, string gardenUuid, string? plotGroupUuid = null)
		{
			Log.Debug("CropSelectionMenu: OpenForPlot called with plotUuid: {PlotUuid}, gardenUuid: {GardenUuid}", plotUuid, gardenUuid);

			if (string.IsNullOrEmpty(plotUuid))
			{
				Log.Error("CropSelectionMenu: OpenForPlot called with null or empty plotUuid!");
				return;
			}
			if (string.IsNullOrEmpty(gardenUuid))
			{
				Log.Error("CropSelectionMenu: OpenForPlot called with null or empty gardenUuid!");
				return;
			}

			// Set UUIDs first before anything else
			_plotUuid = plotUuid;
			_gardenUuid = gardenUuid;
			_plotGroupUuid = plotGroupUuid;

			Log.Debug("CropSelectionMenu: UUIDs set - plotUuid: {PlotUuid}, gardenUuid: {GardenUuid}", _plotUuid, _gardenUuid);

			// Ensure initialization is complete
			if (_plotService == null)
			{
				var apiClient = GetNode<ApiClient>("/root/ApiClient");
				_plotService = new PlotService(apiClient);
			}
			if (_timeManager == null)
			{
				_timeManager = GetNodeOrNull<TimeManager>("/root/TimeManager");
			}

			// Ensure UI structure exists
			if (_container == null || _cropsContainer == null)
			{
				Log.Warning("CropSelectionMenu: UI structure not ready, waiting for _Ready()");
				// Wait for _Ready to complete
				await GetTree().ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				await GetTree().ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			}

			// Show this menu as a submenu (don't call ShowMenu which changes mouse mode)
			// The plot menu should already be visible and handling mouse mode
			Visible = true;
			_isOpen = true;

			// Wait a frame to ensure UI is ready
			await GetTree().ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

			// Double-check UUIDs are still set
			if (string.IsNullOrEmpty(_plotUuid))
			{
				Log.Error("CropSelectionMenu: _plotUuid became null after showing menu!");
				return;
			}

			await LoadCrops();
		}

		/// <summary>
		/// Load suitable crops from API endpoint
		/// </summary>
		private async Task LoadCrops()
		{
			if (_cropsContainer == null)
			{
				Log.Warning("CropSelectionMenu: Cannot load crops - _cropsContainer is null");
				return;
			}
			if (_plotService == null)
			{
				Log.Warning("CropSelectionMenu: Cannot load crops - _plotService is null");
				return;
			}
			if (string.IsNullOrEmpty(_plotUuid))
			{
				Log.Warning("CropSelectionMenu: Cannot load crops - _plotUuid is null or empty: '{PlotUuid}'", _plotUuid ?? "null");
				return;
			}
			if (_timeManager == null)
			{
				Log.Warning("CropSelectionMenu: Cannot load crops - _timeManager is null");
				return;
			}

			// Clear existing crop buttons
			foreach (Node child in _cropsContainer.GetChildren())
			{
				child.QueueFree();
			}

			// Show loading message
			var loadingLabel = new Label();
			loadingLabel.Text = "Loading crops...";
			loadingLabel.HorizontalAlignment = HorizontalAlignment.Center;
			_cropsContainer.AddChild(loadingLabel);

			// Wait a frame to avoid "Busy" HTTP error (like garden loading does)
			await GetTree().ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

			// Get current year from TimeManager
			int year = _timeManager.CurrentYear;

			// Fetch suitable crops from API (returns CropSuitability with rotation scores)
			var suitableCrops = await _plotService.GetSuitableCropsAsync(_plotUuid, year);

			// Remove loading label
			loadingLabel.QueueFree();

			if (suitableCrops == null || suitableCrops.Count == 0)
			{
				var noCropsLabel = new Label();
				noCropsLabel.Text = "No suitable crops available";
				noCropsLabel.HorizontalAlignment = HorizontalAlignment.Center;
				_cropsContainer.AddChild(noCropsLabel);
				return;
			}

			// Sort by rotation score (higher is better) and rotation_allowed
			var sortedCrops = suitableCrops
				.OrderByDescending(sc => sc.RotationAllowed)
				.ThenByDescending(sc => sc.RotationScore ?? 0)
				.ToList();

			// Create buttons for each crop
			foreach (var cropSuitability in sortedCrops)
			{
				var crop = cropSuitability.Crop;
				var cropButton = new Button();
				cropButton.Text = FormatCropButtonText(crop, cropSuitability);
				cropButton.Pressed += () => OnCropSelected(crop);
				_cropsContainer.AddChild(cropButton);
			}
		}

		/// <summary>
		/// Format crop button text with additional info including suitability
		/// </summary>
		private string FormatCropButtonText(Crop crop, CropSuitability? suitability = null)
		{
			var text = crop.Name;
			if (crop.CropFamily != null)
			{
				text += $" ({crop.CropFamily.Name})";
			}
			if (!string.IsNullOrEmpty(crop.RotationGroup) && crop.RotationGroup != "misc")
			{
				text += $" [{crop.RotationGroup}]";
			}
			if (suitability != null)
			{
				if (suitability.RotationScore.HasValue)
				{
					text += $" Score: {suitability.RotationScore}";
				}
				if (!string.IsNullOrEmpty(suitability.SoilIndicator))
				{
					text += $" Soil: {suitability.SoilIndicator}";
				}
			}
			return text;
		}

		/// <summary>
		/// Handle crop selection
		/// </summary>
		private async void OnCropSelected(Crop crop)
		{
			Log.Debug("CropSelectionMenu: Crop selected: {CropName}", crop.Name);

			if (_plotService == null || string.IsNullOrEmpty(_plotUuid) || string.IsNullOrEmpty(_gardenUuid))
			{
				Log.Error("CropSelectionMenu: Cannot plant crop - missing required data");
				return;
			}

			// Get current date from TimeManager
			string datePlanted = GetCurrentDate();

			// Create plant data
			var plantData = new Dictionary<string, object>
			{
				{ "plot_uuid", _plotUuid },
				{ "crop_uuid", crop.CropUuid },
				{ "date_planted", datePlanted },
				{ "method", "direct_sowing" } // Default method, could be made configurable
			};

			// Plant the crop
			var planted = await _plotService.PlantCropAsync(_gardenUuid, plantData);

			if (planted != null)
			{
				Log.Information("CropSelectionMenu: Successfully planted {CropName}", crop.Name);
				// Go back to plot menu instead of closing
				Visible = false;
				var uiCanvas = GetTree().CurrentScene.GetNodeOrNull<CanvasLayer>("UICanvas");
				var plotMenu = uiCanvas?.GetNodeOrNull<PlotInteractionMenu>("PlotInteractionMenu");
				if (plotMenu != null && !string.IsNullOrEmpty(_plotUuid))
				{
					plotMenu.OpenForPlot(_plotUuid);
				}
			}
			else
			{
				Log.Warning("CropSelectionMenu: Failed to plant {CropName}", crop.Name);
				// Could show error message to user
			}
		}

		/// <summary>
		/// Get current date as YYYY-MM-DD string
		/// </summary>
		private string GetCurrentDate()
		{
			if (_timeManager == null)
			{
				return System.DateTime.Now.ToString("yyyy-MM-dd");
			}

			// Use TimeManager's current month and year
			int month = _timeManager.CurrentMonth;
			int year = _timeManager.CurrentYear;
			// Use first day of month as default (could be improved to use actual day)
			return $"{year}-{month:D2}-01";
		}

		/// <summary>
		/// Handle back button press - go back to plot interaction menu (just hide this submenu)
		/// </summary>
		private void OnBackPressed()
		{
			Log.Debug("CropSelectionMenu: Back button pressed, hiding submenu");
			
			// Just hide this submenu - the plot menu should already be visible behind it
			Visible = false;
			_isOpen = false; // Update internal state
			
			// Don't change mouse mode - let the plot menu handle it
		}

		public override void HideMenu()
		{
			base.HideMenu();
			// Don't clear UUIDs here - they might be needed if menu is reopened
			// Only clear when explicitly closing, not when going back to plot menu
			
			// Clear crop buttons
			if (_cropsContainer != null)
			{
				foreach (Node child in _cropsContainer.GetChildren())
				{
					child.QueueFree();
				}
			}
		}
	}
}

