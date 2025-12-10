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


