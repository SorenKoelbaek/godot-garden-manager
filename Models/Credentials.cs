using System.Text.Json.Serialization;

namespace GardenManager.Models
{
    public class Credentials
    {
        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;

        [JsonPropertyName("password")]
        public string Password { get; set; } = string.Empty;

        [JsonPropertyName("game_key")]
        public string GameKey { get; set; } = string.Empty;
    }
}

