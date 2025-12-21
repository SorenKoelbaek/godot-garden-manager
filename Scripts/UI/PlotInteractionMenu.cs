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
	public partial class PlotInteractionMenu : BaseMenu
	{
		private TabContainer? _tabContainer;
		private VBoxContainer? _detailsTab;
		private VBoxContainer? _actionsTab;
		private Label? _plotNameLabel;
		private Label? _plotTypeLabel;
		private ScrollContainer? _detailsScroll;
		private VBoxContainer? _detailsContent;
		private VBoxContainer? _actionsContainer;
		private PlotService? _plotService;
		private PlotEventsView? _eventsView;
		private CropSelectionView? _cropSelectionView;
		private Plot? _currentPlot;
		private string? _currentPlotUuid;
		private string? _currentGardenUuid;

		public override void _Ready()
		{
			Log.Debug("PlotInteractionMenu: _Ready() called");

			// Get API client and create plot service
			var apiClient = GetNode<ApiClient>("/root/ApiClient");
			_plotService = new PlotService(apiClient);

			// Get GameManager for garden UUID
			var gameManager = GetNodeOrNull<GameManager>("/root/GameManager");
			if (gameManager != null)
			{
				_currentGardenUuid = gameManager.CurrentGardenUuid;
			}

			// Create UI structure
			AnchorsPreset = 15;
			Visible = false;

			_tabContainer = new TabContainer();
			_tabContainer.AnchorsPreset = 8; // Center preset
			_tabContainer.AnchorLeft = 0.5f;
			_tabContainer.AnchorTop = 0.5f;
			_tabContainer.AnchorRight = 0.5f;
			_tabContainer.AnchorBottom = 0.5f;
			_tabContainer.OffsetLeft = -250;
			_tabContainer.OffsetTop = -300;
			_tabContainer.OffsetRight = 250;
			_tabContainer.OffsetBottom = 300;
			AddChild(_tabContainer);

			// Details Tab
			_detailsTab = new VBoxContainer();
			_detailsTab.Name = "Details";
			_tabContainer.AddChild(_detailsTab);

			_plotNameLabel = new Label();
			_plotNameLabel.Text = "Plot Name";
			_plotNameLabel.HorizontalAlignment = HorizontalAlignment.Center;
			_detailsTab.AddChild(_plotNameLabel);

			_plotTypeLabel = new Label();
			_plotTypeLabel.Text = "Plot Type";
			_plotTypeLabel.HorizontalAlignment = HorizontalAlignment.Center;
			_detailsTab.AddChild(_plotTypeLabel);

			var separator1 = new HSeparator();
			_detailsTab.AddChild(separator1);

			_detailsScroll = new ScrollContainer();
			_detailsScroll.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
			_detailsTab.AddChild(_detailsScroll);

			_detailsContent = new VBoxContainer();
			_detailsContent.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			_detailsScroll.AddChild(_detailsContent);

			// Actions Tab
			_actionsTab = new VBoxContainer();
			_actionsTab.Name = "Actions";
			_tabContainer.AddChild(_actionsTab);

			_actionsContainer = new VBoxContainer();
			_actionsTab.AddChild(_actionsContainer);

			Log.Debug("PlotInteractionMenu: UI structure created");
		}

		/// <summary>
		/// Open menu for a specific plot
		/// </summary>
		public async void OpenForPlot(string plotUuid)
		{
			_currentPlotUuid = plotUuid;
			ShowMenu();

			// Load plot details
			if (_plotService != null)
			{
				_currentPlot = await _plotService.GetPlotDetailsAsync(plotUuid);
				UpdatePlotInfo();
				UpdateDetailsTab();
				await LoadActions();
			}
		}

		/// <summary>
		/// Update plot information display
		/// </summary>
		private void UpdatePlotInfo()
		{
			if (_plotNameLabel == null || _plotTypeLabel == null)
			{
				return;
			}

			if (_currentPlot != null)
			{
				_plotNameLabel.Text = _currentPlot.Name;
				_plotTypeLabel.Text = _currentPlot.PlotType?.Name ?? "Unknown Type";
			}
			else
			{
				_plotNameLabel.Text = "Plot";
				_plotTypeLabel.Text = "Unknown";
			}
		}

		/// <summary>
		/// Update Details tab with plot data, current plant, and events
		/// </summary>
		private void UpdateDetailsTab()
		{
			if (_detailsContent == null)
			{
				return;
			}

			// Clear existing content
			foreach (Node child in _detailsContent.GetChildren())
			{
				child.QueueFree();
			}

			if (_currentPlot == null)
			{
				return;
			}

			// Plot dimensions
			var dimensionsLabel = new Label();
			dimensionsLabel.Text = $"Size: {_currentPlot.Width} x {_currentPlot.Depth} {_currentPlot.Unit}";
			dimensionsLabel.HorizontalAlignment = HorizontalAlignment.Center;
			_detailsContent.AddChild(dimensionsLabel);

			var separator2 = new HSeparator();
			_detailsContent.AddChild(separator2);

			// Current Plant Section
			var plantSectionLabel = new Label();
			plantSectionLabel.Text = "Current Plant";
			plantSectionLabel.HorizontalAlignment = HorizontalAlignment.Center;
			_detailsContent.AddChild(plantSectionLabel);

			if (_currentPlot.CurrentPlanted != null && _currentPlot.CurrentPlanted.Count > 0)
			{
				foreach (var planted in _currentPlot.CurrentPlanted)
				{
					var plantInfo = new VBoxContainer();
					plantInfo.AddThemeConstantOverride("separation", 2);

					var cropNameLabel = new Label();
					cropNameLabel.Text = planted.Crop?.Name ?? "Unknown Crop";
					plantInfo.AddChild(cropNameLabel);

					var dateLabel = new Label();
					dateLabel.Text = $"Planted: {planted.DatePlanted}";
					plantInfo.AddChild(dateLabel);

					if (planted.Amount.HasValue)
					{
						var amountLabel = new Label();
						amountLabel.Text = $"Amount: {planted.Amount}";
						plantInfo.AddChild(amountLabel);
					}

					if (!string.IsNullOrEmpty(planted.Method))
					{
						var methodLabel = new Label();
						methodLabel.Text = $"Method: {planted.Method}";
						plantInfo.AddChild(methodLabel);
					}

					var plantSeparator = new HSeparator();
					plantInfo.AddChild(plantSeparator);

					_detailsContent.AddChild(plantInfo);
				}
			}
			else
			{
				var noPlantLabel = new Label();
				noPlantLabel.Text = "No plants currently in this plot";
				noPlantLabel.HorizontalAlignment = HorizontalAlignment.Center;
				_detailsContent.AddChild(noPlantLabel);
			}

			var separator3 = new HSeparator();
			_detailsContent.AddChild(separator3);

			// Events Section (Upcoming)
			var eventsSectionLabel = new Label();
			eventsSectionLabel.Text = "Upcoming Events";
			eventsSectionLabel.HorizontalAlignment = HorizontalAlignment.Center;
			_detailsContent.AddChild(eventsSectionLabel);

			if (_currentPlot.Events != null && _currentPlot.Events.Count > 0)
			{
				var upcomingEvents = _currentPlot.Events.Where(e =>
					e.Status != "done" && string.IsNullOrEmpty(e.DateExecuted)
				).OrderBy(e => e.WindowStart ?? "").ToList();

				if (upcomingEvents.Count > 0)
				{
					foreach (var evt in upcomingEvents)
					{
						var eventInfo = new VBoxContainer();
						eventInfo.AddThemeConstantOverride("separation", 2);

						var eventTypeLabel = new Label();
						eventTypeLabel.Text = evt.EventType?.Name ?? "Unknown Event";
						eventTypeLabel.AddThemeColorOverride("font_color", Colors.Orange);
						eventInfo.AddChild(eventTypeLabel);

						if (!string.IsNullOrEmpty(evt.WindowStart))
						{
							var dateLabel = new Label();
							dateLabel.Text = $"Due: {evt.WindowStart}";
							if (!string.IsNullOrEmpty(evt.WindowEnd))
							{
								dateLabel.Text += $" - {evt.WindowEnd}";
							}
							eventInfo.AddChild(dateLabel);
						}

						if (!string.IsNullOrEmpty(evt.Notes))
						{
							var notesLabel = new Label();
							notesLabel.Text = evt.Notes;
							notesLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
							eventInfo.AddChild(notesLabel);
						}

						var eventSeparator = new HSeparator();
						eventInfo.AddChild(eventSeparator);

						_detailsContent.AddChild(eventInfo);
					}
				}
				else
				{
					var noEventsLabel = new Label();
					noEventsLabel.Text = "No upcoming events";
					noEventsLabel.HorizontalAlignment = HorizontalAlignment.Center;
					_detailsContent.AddChild(noEventsLabel);
				}
			}
			else
			{
				var noEventsLabel = new Label();
				noEventsLabel.Text = "No events";
				noEventsLabel.HorizontalAlignment = HorizontalAlignment.Center;
				_detailsContent.AddChild(noEventsLabel);
			}
		}

		/// <summary>
		/// Load and display plot actions
		/// </summary>
		private async Task LoadActions()
		{
			if (_actionsContainer == null || _plotService == null || string.IsNullOrEmpty(_currentPlotUuid))
			{
				return;
			}

			// Clear existing action buttons
			foreach (Node child in _actionsContainer.GetChildren())
			{
				child.QueueFree();
			}

			// Check if this is a vegetable plot (has plot_type)
			bool isVegetablePlot = _currentPlot?.PlotType != null;

			if (isVegetablePlot)
			{
				// Vegetable plot actions
				var showInfoButton = new Button();
				showInfoButton.Text = "Show Information";
				showInfoButton.Pressed += OnShowInformation;
				_actionsContainer.AddChild(showInfoButton);

				var plantButton = new Button();
				plantButton.Text = "Plant a new crop";
				plantButton.Pressed += OnPlantCrop;
				_actionsContainer.AddChild(plantButton);

				var seeActionsButton = new Button();
				seeActionsButton.Text = "See actions";
				seeActionsButton.Pressed += OnSeeActions;
				_actionsContainer.AddChild(seeActionsButton);
			}
			else
			{
				// Default actions for other plot types
				var defaultActions = new[] { "View Details", "Harvest", "Water" };
				foreach (var action in defaultActions)
				{
					var button = new Button();
					button.Text = action;
					button.Pressed += () => OnActionPressed(action);
					_actionsContainer.AddChild(button);
				}
			}
		}

		/// <summary>
		/// Handle "Show Information" action - switch to Details tab
		/// </summary>
		private void OnShowInformation()
		{
			if (_tabContainer != null)
			{
				_tabContainer.CurrentTab = 0; // Switch to Details tab
			}
		}

		/// <summary>
		/// Handle "Plant a new crop" action - show crop selection view in Actions tab
		/// </summary>
		private async void OnPlantCrop()
		{
			if (_actionsContainer == null || _plotService == null || string.IsNullOrEmpty(_currentPlotUuid) || string.IsNullOrEmpty(_currentGardenUuid))
			{
				Log.Warning("PlotInteractionMenu: Cannot open crop selection - missing required data");
				return;
			}

			// Clear existing content in actions tab
			foreach (Node child in _actionsContainer.GetChildren())
			{
				child.QueueFree();
			}

			// Create crop selection view
			_cropSelectionView = new CropSelectionView();
			_cropSelectionView.Name = "CropSelectionView";
			_cropSelectionView.BackPressed += OnCropSelectionBackPressed;
			
			// Add to container first so _Ready() gets called
			_actionsContainer.AddChild(_cropSelectionView);
			
			// Wait for _Ready to complete
			await GetTree().ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await GetTree().ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

			// Load and show crops
			await _cropSelectionView.LoadCropsAsync(_plotService, _currentPlotUuid, _currentGardenUuid, _currentPlot?.PlotGroupUuid);

			// Switch to Actions tab
			if (_tabContainer != null)
			{
				_tabContainer.CurrentTab = 1; // Switch to Actions tab
			}
		}

		/// <summary>
		/// Handle back button from crop selection view - restore action buttons
		/// </summary>
		private async void OnCropSelectionBackPressed()
		{
			// Remove crop selection view
			if (_cropSelectionView != null && _cropSelectionView.GetParent() != null)
			{
				_cropSelectionView.GetParent().RemoveChild(_cropSelectionView);
			}

			// Reload action buttons
			await LoadActions();
		}

		/// <summary>
		/// Handle "See actions" - show events view
		/// </summary>
		private async void OnSeeActions()
		{
			if (_plotService == null || string.IsNullOrEmpty(_currentPlotUuid))
			{
				return;
			}

			// Get events for the plot
			var events = await _plotService.GetPlotEventsAsync(_currentPlotUuid);

			// Create or get events view
			if (_eventsView == null)
			{
				_eventsView = new PlotEventsView();
				_eventsView.Name = "PlotEventsView";
				_eventsView.BackPressed += OnEventsViewBackPressed;
			}

			// Clear existing content in actions tab and show events
			foreach (Node child in _actionsContainer.GetChildren())
			{
				child.QueueFree();
			}

			_eventsView.UpdateEvents(events);
			_actionsContainer.AddChild(_eventsView);

			// Switch to Actions tab
			if (_tabContainer != null)
			{
				_tabContainer.CurrentTab = 1; // Switch to Actions tab
			}
		}

		/// <summary>
		/// Handle back button from events view - restore action buttons
		/// </summary>
		private async void OnEventsViewBackPressed()
		{
			// Remove events view
			if (_eventsView != null && _eventsView.GetParent() != null)
			{
				_eventsView.GetParent().RemoveChild(_eventsView);
			}

			// Reload action buttons
			await LoadActions();
		}

		/// <summary>
		/// Handle action button press (for non-vegetable plots)
		/// </summary>
		private async void OnActionPressed(string action)
		{
			Log.Debug("PlotInteractionMenu: Action pressed: {Action}", action);

			if (_plotService == null || string.IsNullOrEmpty(_currentPlotUuid))
			{
				return;
			}

			// Execute action
			var success = await _plotService.ExecutePlotActionAsync(_currentPlotUuid, action);

			if (success)
			{
				Log.Information("PlotInteractionMenu: Action '{Action}' executed successfully", action);
			}
			else
			{
				Log.Warning("PlotInteractionMenu: Action '{Action}' failed", action);
			}
		}

		public override void HideMenu()
		{
			base.HideMenu();
			_currentPlot = null;
			_currentPlotUuid = null;

			// Clear action buttons
			if (_actionsContainer != null)
			{
				foreach (Node child in _actionsContainer.GetChildren())
				{
					if (child is not PlotEventsView)
					{
						child.QueueFree();
					}
				}
			}

			// Remove events view if present
			if (_eventsView != null && _eventsView.GetParent() != null)
			{
				_eventsView.GetParent().RemoveChild(_eventsView);
				_eventsView = null;
			}

			// Remove crop selection view if present
			if (_cropSelectionView != null && _cropSelectionView.GetParent() != null)
			{
				_cropSelectionView.GetParent().RemoveChild(_cropSelectionView);
				_cropSelectionView = null;
			}
		}

		public override void ShowMenu()
		{
			base.ShowMenu();
			// Switch to Details tab by default
			if (_tabContainer != null)
			{
				_tabContainer.CurrentTab = 0;
			}
		}
	}
}
