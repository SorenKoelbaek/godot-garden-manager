using System.Text.Json.Serialization;

namespace GardenManager.Models
{
	public class Planted
	{
		[JsonPropertyName("planted_uuid")]
		public string PlantedUuid { get; set; } = string.Empty;

		[JsonPropertyName("plot_uuid")]
		public string PlotUuid { get; set; } = string.Empty;

		[JsonPropertyName("crop_uuid")]
		public string CropUuid { get; set; } = string.Empty;

		[JsonPropertyName("date_planted")]
		public string DatePlanted { get; set; } = string.Empty;

		[JsonPropertyName("rows")]
		public int? Rows { get; set; }

		[JsonPropertyName("amount")]
		public int? Amount { get; set; }

		[JsonPropertyName("method")]
		public string Method { get; set; } = string.Empty;

		[JsonPropertyName("orientation")]
		public string? Orientation { get; set; }

		[JsonPropertyName("placement_mode")]
		public string? PlacementMode { get; set; }

		[JsonPropertyName("plot")]
		public Plot? Plot { get; set; }

		[JsonPropertyName("crop")]
		public Crop? Crop { get; set; }
	}

	public class PlantedSimple
	{
		[JsonPropertyName("planted_uuid")]
		public string PlantedUuid { get; set; } = string.Empty;

		[JsonPropertyName("plot_uuid")]
		public string PlotUuid { get; set; } = string.Empty;

		[JsonPropertyName("crop_uuid")]
		public string CropUuid { get; set; } = string.Empty;

		[JsonPropertyName("date_planted")]
		public string DatePlanted { get; set; } = string.Empty;

		[JsonPropertyName("rows")]
		public int? Rows { get; set; }

		[JsonPropertyName("amount")]
		public int? Amount { get; set; }

		[JsonPropertyName("method")]
		public string Method { get; set; } = string.Empty;

		[JsonPropertyName("orientation")]
		public string? Orientation { get; set; }

		[JsonPropertyName("placement_mode")]
		public string? PlacementMode { get; set; }

		[JsonPropertyName("crop")]
		public Crop? Crop { get; set; }
	}
}

