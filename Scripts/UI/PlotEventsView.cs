#nullable enable
using System.Collections.Generic;
using System.Linq;
using GardenManager.Models;
using Godot;
using Serilog;

namespace GardenManager.UI
{
	public partial class PlotEventsView : VBoxContainer
	{
		private VBoxContainer? _upcomingContainer;
		private VBoxContainer? _doneContainer;
		private Label? _upcomingLabel;
		private Label? _doneLabel;
		private Button? _backButton;

		[Signal]
		public delegate void BackPressedEventHandler();

		public override void _Ready()
		{
			Log.Debug("PlotEventsView: _Ready() called");

			// Back button at the top
			_backButton = new Button();
			_backButton.Text = "Back";
			_backButton.Pressed += OnBackPressed;
			AddChild(_backButton);

			var separator0 = new HSeparator();
			AddChild(separator0);

			// Upcoming tasks section
			_upcomingLabel = new Label();
			_upcomingLabel.Text = "Upcoming Tasks";
			_upcomingLabel.HorizontalAlignment = HorizontalAlignment.Center;
			AddChild(_upcomingLabel);

			_upcomingContainer = new VBoxContainer();
			AddChild(_upcomingContainer);

			var separator = new HSeparator();
			AddChild(separator);

			// Done tasks section
			_doneLabel = new Label();
			_doneLabel.Text = "Done Tasks";
			_doneLabel.HorizontalAlignment = HorizontalAlignment.Center;
			AddChild(_doneLabel);

			_doneContainer = new VBoxContainer();
			AddChild(_doneContainer);

			Log.Debug("PlotEventsView: Initialized");
		}

		/// <summary>
		/// Update the view with events
		/// </summary>
		public void UpdateEvents(List<Event>? events)
		{
			if (_upcomingContainer == null || _doneContainer == null)
			{
				return;
			}

			// Clear existing items
			foreach (Node child in _upcomingContainer.GetChildren())
			{
				child.QueueFree();
			}
			foreach (Node child in _doneContainer.GetChildren())
			{
				child.QueueFree();
			}

			if (events == null || events.Count == 0)
			{
				var noEventsLabel = new Label();
				noEventsLabel.Text = "No events found";
				noEventsLabel.HorizontalAlignment = HorizontalAlignment.Center;
				_upcomingContainer.AddChild(noEventsLabel);
				return;
			}

			// Separate upcoming and done events
			var upcomingEvents = events.Where(e => 
				e.Status != "done" && string.IsNullOrEmpty(e.DateExecuted)
			).OrderBy(e => e.WindowStart ?? "").ToList();

			var doneEvents = events.Where(e => 
				e.Status == "done" || !string.IsNullOrEmpty(e.DateExecuted)
			).OrderByDescending(e => e.DateExecuted ?? "").ToList();

			// Display upcoming events
			if (upcomingEvents.Count == 0)
			{
				var noUpcomingLabel = new Label();
				noUpcomingLabel.Text = "No upcoming tasks";
				noUpcomingLabel.HorizontalAlignment = HorizontalAlignment.Center;
				_upcomingContainer.AddChild(noUpcomingLabel);
			}
			else
			{
				foreach (var evt in upcomingEvents)
				{
					var eventItem = CreateEventItem(evt, false);
					_upcomingContainer.AddChild(eventItem);
				}
			}

			// Display done events
			if (doneEvents.Count == 0)
			{
				var noDoneLabel = new Label();
				noDoneLabel.Text = "No completed tasks";
				noDoneLabel.HorizontalAlignment = HorizontalAlignment.Center;
				_doneContainer.AddChild(noDoneLabel);
			}
			else
			{
				foreach (var evt in doneEvents)
				{
					var eventItem = CreateEventItem(evt, true);
					_doneContainer.AddChild(eventItem);
				}
			}
		}

		/// <summary>
		/// Create a UI item for an event
		/// </summary>
		private Control CreateEventItem(Event evt, bool isDone)
		{
			var container = new VBoxContainer();
			container.AddThemeConstantOverride("separation", 2);

			// Event type name
			var typeLabel = new Label();
			typeLabel.Text = evt.EventType?.Name ?? "Unknown Event";
			typeLabel.AddThemeColorOverride("font_color", isDone ? Colors.Gray : Colors.Orange);
			container.AddChild(typeLabel);

			// Date information
			if (!string.IsNullOrEmpty(evt.WindowStart) || !string.IsNullOrEmpty(evt.DateExecuted))
			{
				var dateLabel = new Label();
				if (isDone && !string.IsNullOrEmpty(evt.DateExecuted))
				{
					dateLabel.Text = $"Completed: {evt.DateExecuted}";
				}
				else if (!string.IsNullOrEmpty(evt.WindowStart))
				{
					dateLabel.Text = $"Due: {evt.WindowStart}";
					if (!string.IsNullOrEmpty(evt.WindowEnd))
					{
						dateLabel.Text += $" - {evt.WindowEnd}";
					}
				}
				dateLabel.AddThemeColorOverride("font_color", isDone ? Colors.Gray : Colors.White);
				container.AddChild(dateLabel);
			}

			// Notes
			if (!string.IsNullOrEmpty(evt.Notes))
			{
				var notesLabel = new Label();
				notesLabel.Text = evt.Notes;
				notesLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
				notesLabel.AddThemeColorOverride("font_color", isDone ? Colors.Gray : Colors.White);
				container.AddChild(notesLabel);
			}

			// Status
			if (!string.IsNullOrEmpty(evt.Status))
			{
				var statusLabel = new Label();
				statusLabel.Text = $"Status: {evt.Status}";
				statusLabel.AddThemeColorOverride("font_color", isDone ? Colors.Gray : Colors.Yellow);
				container.AddChild(statusLabel);
			}

			var separator = new HSeparator();
			container.AddChild(separator);

			return container;
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

