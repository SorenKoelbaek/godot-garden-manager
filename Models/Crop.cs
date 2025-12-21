using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GardenManager.Models
{
	public class Crop
	{
		[JsonPropertyName("crop_uuid")]
		public string CropUuid { get; set; } = string.Empty;

		[JsonPropertyName("name")]
		public string Name { get; set; } = string.Empty;

		[JsonPropertyName("description")]
		public string? Description { get; set; }

		[JsonPropertyName("latin_name")]
		public string? LatinName { get; set; }

		[JsonPropertyName("crop_type_uuid")]
		public string CropTypeUuid { get; set; } = string.Empty;

		[JsonPropertyName("harvest_min_days")]
		public int? HarvestMinDays { get; set; }

		[JsonPropertyName("harvest_max_days")]
		public int? HarvestMaxDays { get; set; }

		[JsonPropertyName("plant_spacing_cm")]
		public int? PlantSpacingCm { get; set; }

		[JsonPropertyName("row_spacing_cm")]
		public int? RowSpacingCm { get; set; }

		[JsonPropertyName("soil_effect")]
		public string SoilEffect { get; set; } = "neutral";

		[JsonPropertyName("rotation_group")]
		public string RotationGroup { get; set; } = "misc";

		[JsonPropertyName("crop_family")]
		public CropFamily? CropFamily { get; set; }

		[JsonPropertyName("crop_type")]
		public CropType? CropType { get; set; }

		[JsonPropertyName("crop_windows")]
		public List<CropWindow> CropWindows { get; set; } = new List<CropWindow>();
	}

	public class CropWindow
	{
		[JsonPropertyName("crop_window_uuid")]
		public string CropWindowUuid { get; set; } = string.Empty;

		[JsonPropertyName("crop_uuid")]
		public string CropUuid { get; set; } = string.Empty;

		[JsonPropertyName("period_type")]
		public string PeriodType { get; set; } = string.Empty;

		[JsonPropertyName("window_start")]
		public string WindowStart { get; set; } = string.Empty;

		[JsonPropertyName("window_end")]
		public string WindowEnd { get; set; } = string.Empty;

		[JsonPropertyName("notes")]
		public string? Notes { get; set; }
	}

	public class CropFamily
	{
		[JsonPropertyName("crop_family_uuid")]
		public string CropFamilyUuid { get; set; } = string.Empty;

		[JsonPropertyName("name")]
		public string Name { get; set; } = string.Empty;

		[JsonPropertyName("description")]
		public string? Description { get; set; }

		[JsonPropertyName("common_traits")]
		public Dictionary<string, object>? CommonTraits { get; set; }
	}

	public class CropType
	{
		[JsonPropertyName("crop_type_uuid")]
		public string CropTypeUuid { get; set; } = string.Empty;

		[JsonPropertyName("name")]
		public string Name { get; set; } = string.Empty;

		[JsonPropertyName("description")]
		public string? Description { get; set; }

		[JsonPropertyName("allowed_period_types")]
		public List<string> AllowedPeriodTypes { get; set; } = new List<string>();

		[JsonPropertyName("created_at")]
		public string CreatedAt { get; set; } = string.Empty;
	}
}

