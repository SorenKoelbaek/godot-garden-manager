using System.Text.Json.Serialization;

namespace GardenManager.Models
{
	public class Garden
	{
		[JsonPropertyName("garden_uuid")]
		public string GardenUuid { get; set; } = string.Empty;

		[JsonPropertyName("name")]
		public string Name { get; set; } = string.Empty;

		[JsonPropertyName("width")]
		public double Width { get; set; }

		[JsonPropertyName("depth")]
		public double Depth { get; set; }

		[JsonPropertyName("unit")]
		public string Unit { get; set; } = "meters";
	}
}
