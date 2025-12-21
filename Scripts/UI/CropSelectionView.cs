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
	public partial class CropSelectionView : VBoxContainer
	{
		private ScrollContainer? _scrollContainer;
		private VBoxContainer? _cropsContainer;
		private Button? _backButton;
		private TimeManager? _timeManager;

		[Signal]
		public delegate void BackPressedEventHandler();

		public override void _Ready()
		{
			Log.Debug("CropSelectionView: _Ready() called");

			// Get TimeManager for current year
			_timeManager = GetNodeOrNull<TimeManager>("/root/TimeManager");
			if (_timeManager == null)
			{
				Log.Warning("CropSelectionView: TimeManager not found in _Ready(), will retry in LoadCropsAsync");
			}

			// Back button at the top
			_backButton = new Button();
			_backButton.Text = "Back";
			_backButton.Pressed += OnBackPressed;
			AddChild(_backButton);

			var separator0 = new HSeparator();
			AddChild(separator0);

			var titleLabel = new Label();
			titleLabel.Text = "Select Crop to Plant";
			titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
			AddChild(titleLabel);

			var separator = new HSeparator();
			AddChild(separator);

			_scrollContainer = new ScrollContainer();
			_scrollContainer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
			AddChild(_scrollContainer);

			_cropsContainer = new VBoxContainer();
			_cropsContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			_scrollContainer.AddChild(_cropsContainer);

			Log.Debug("CropSelectionView: Initialized");
		}

		/// <summary>
		/// Load suitable crops from API endpoint
		/// </summary>
		public async Task LoadCropsAsync(PlotService plotService, string plotUuid, string gardenUuid, string? plotGroupUuid = null)
		{
			Log.Debug("CropSelectionView: LoadCropsAsync called with plotUuid: {PlotUuid}", plotUuid);

			// Ensure UI structure is initialized (in case _Ready hasn't been called yet)
			if (_cropsContainer == null)
			{
				Log.Warning("CropSelectionView: UI structure not initialized, initializing now");
				// Wait for _Ready to complete
				await GetTree().ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				await GetTree().ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				
				if (_cropsContainer == null)
				{
					Log.Error("CropSelectionView: _cropsContainer still null after waiting for _Ready");
					return;
				}
			}

			if (plotService == null)
			{
				Log.Warning("CropSelectionView: Cannot load crops - plotService is null");
				return;
			}
			if (string.IsNullOrEmpty(plotUuid))
			{
				Log.Warning("CropSelectionView: Cannot load crops - plotUuid is null or empty: '{PlotUuid}'", plotUuid ?? "null");
				return;
			}
			if (_timeManager == null)
			{
				Log.Warning("CropSelectionView: Cannot load crops - _timeManager is null, retrying...");
				// Try to get it again
				_timeManager = GetNodeOrNull<TimeManager>("/root/TimeManager");
				if (_timeManager == null)
				{
					Log.Error("CropSelectionView: TimeManager not found even after retry");
					return;
				}
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
			var suitableCrops = await plotService.GetSuitableCropsAsync(plotUuid, year);

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
				cropButton.Pressed += () => OnCropSelected(plotService, crop, plotUuid, gardenUuid);
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
		private async void OnCropSelected(PlotService plotService, Crop crop, string plotUuid, string gardenUuid)
		{
			Log.Debug("CropSelectionView: Crop selected: {CropName}", crop.Name);

			if (string.IsNullOrEmpty(plotUuid) || string.IsNullOrEmpty(gardenUuid))
			{
				Log.Error("CropSelectionView: Cannot plant crop - missing required data");
				return;
			}

			// Get current date from TimeManager
			string datePlanted = GetCurrentDate();

			// Create plant data
			var plantData = new Dictionary<string, object>
			{
				{ "plot_uuid", plotUuid },
				{ "crop_uuid", crop.CropUuid },
				{ "date_planted", datePlanted },
				{ "method", "direct_sowing" } // Default method, could be made configurable
			};

			// Plant the crop
			var planted = await plotService.PlantCropAsync(gardenUuid, plantData);

			if (planted != null)
			{
				Log.Information("CropSelectionView: Successfully planted {CropName}", crop.Name);
				// Go back to action buttons
				OnBackPressed();
			}
			else
			{
				Log.Warning("CropSelectionView: Failed to plant {CropName}", crop.Name);
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
		/// Handle back button press
		/// </summary>
		private void OnBackPressed()
		{
			EmitSignal(SignalName.BackPressed);
		}
	}
}

