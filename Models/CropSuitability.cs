using System.Text.Json.Serialization;

namespace GardenManager.Models
{
	public class CropSuitability
	{
		[JsonPropertyName("crop")]
		public Crop Crop { get; set; } = null!;

		[JsonPropertyName("rotation_allowed")]
		public bool RotationAllowed { get; set; }

		[JsonPropertyName("rotation_score")]
		public int? RotationScore { get; set; }

		[JsonPropertyName("soil_indicator")]
		public string SoilIndicator { get; set; } = string.Empty;
	}
}

