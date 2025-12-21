using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GardenManager.Models
{
    public class Plot
    {
        [JsonPropertyName("plot_uuid")]
        public string PlotUuid { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("plot_type_uuid")]
        public string PlotTypeUuid { get; set; } = string.Empty;

        [JsonPropertyName("shape")]
        public string Shape { get; set; } = "rectangle";

        [JsonPropertyName("width")]
        public double Width { get; set; }

        [JsonPropertyName("depth")]
        public double Depth { get; set; }

        [JsonPropertyName("unit")]
        public string Unit { get; set; } = "meters";

        [JsonPropertyName("x")]
        public double X { get; set; }

        [JsonPropertyName("y")]
        public double Y { get; set; }

        [JsonPropertyName("rotation")]
        public double Rotation { get; set; } = 0.0;

        [JsonPropertyName("garden_uuid")]
        public string GardenUuid { get; set; } = string.Empty;

        [JsonPropertyName("plot_type")]
        public PlotType? PlotType { get; set; }

        [JsonPropertyName("soil_type_uuid")]
        public string? SoilTypeUuid { get; set; }

        [JsonPropertyName("plot_group_uuid")]
        public string? PlotGroupUuid { get; set; }

        [JsonPropertyName("locked")]
        public bool? Locked { get; set; }

        [JsonPropertyName("events")]
        public List<Event>? Events { get; set; }

        [JsonPropertyName("current_planted")]
        public List<Planted>? CurrentPlanted { get; set; }

        [JsonPropertyName("soil_compositions")]
        public List<Dictionary<string, object>>? SoilCompositions { get; set; }
    }

    public class PlotType
    {
        [JsonPropertyName("plot_type_uuid")]
        public string PlotTypeUuid { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }
}


