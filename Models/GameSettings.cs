using System.Text.Json.Serialization;

namespace GardenManager.Models
{
    public class GameSettings
    {
        [JsonPropertyName("mouse_sensitivity")]
        public float MouseSensitivity { get; set; } = 0.003f;

        [JsonPropertyName("player_speed")]
        public float PlayerSpeed { get; set; } = 5.0f;
    }
}


