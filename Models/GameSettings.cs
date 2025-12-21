using System.Text.Json.Serialization;

namespace GardenManager.Models
{
    public class GameSettings
    {
        [JsonPropertyName("mouse_sensitivity")]
        public float MouseSensitivity { get; set; } = 0.003f;

        [JsonPropertyName("player_speed")]
        public float PlayerSpeed { get; set; } = 5.0f;

        [JsonPropertyName("render_grass")]
        public bool RenderGrass { get; set; } = true;

        [JsonPropertyName("use_advanced_sky")]
        public bool UseAdvancedSky { get; set; } = false;
    }
}


