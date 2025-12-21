using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GardenManager.Models
{
	public class Event
	{
		[JsonPropertyName("event_uuid")]
		public string EventUuid { get; set; } = string.Empty;

		[JsonPropertyName("plot_uuid")]
		public string? PlotUuid { get; set; }

		[JsonPropertyName("event_type_uuid")]
		public string? EventTypeUuid { get; set; }

		[JsonPropertyName("planted_uuid")]
		public string? PlantedUuid { get; set; }

		[JsonPropertyName("window_start")]
		public string? WindowStart { get; set; }

		[JsonPropertyName("window_end")]
		public string? WindowEnd { get; set; }

		[JsonPropertyName("date_executed")]
		public string? DateExecuted { get; set; }

		[JsonPropertyName("notes")]
		public string? Notes { get; set; }

		[JsonPropertyName("status")]
		public string Status { get; set; } = string.Empty;

		[JsonPropertyName("created_at")]
		public string CreatedAt { get; set; } = string.Empty;

		[JsonPropertyName("updated_at")]
		public string UpdatedAt { get; set; } = string.Empty;

		[JsonPropertyName("event_type")]
		public EventType? EventType { get; set; }

		[JsonPropertyName("planted")]
		public PlantedSimple? Planted { get; set; }

		[JsonPropertyName("plot")]
		public PlotSimple? Plot { get; set; }
	}

	public class EventType
	{
		[JsonPropertyName("event_type_uuid")]
		public string EventTypeUuid { get; set; } = string.Empty;

		[JsonPropertyName("name")]
		public string Name { get; set; } = string.Empty;

		[JsonPropertyName("description")]
		public string? Description { get; set; }

		[JsonPropertyName("input_fields")]
		public List<string> InputFields { get; set; } = new List<string>();
	}

	public class PlotSimple
	{
		[JsonPropertyName("plot_uuid")]
		public string PlotUuid { get; set; } = string.Empty;

		[JsonPropertyName("name")]
		public string Name { get; set; } = string.Empty;

		[JsonPropertyName("width")]
		public double Width { get; set; }

		[JsonPropertyName("depth")]
		public double Depth { get; set; }

		[JsonPropertyName("unit")]
		public string Unit { get; set; } = string.Empty;
	}
}

