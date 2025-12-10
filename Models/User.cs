using System.Text.Json.Serialization;

namespace GardenManager.Models
{
    public class User
    {
        [JsonPropertyName("user_uuid")]
        public string UserUuid { get; set; } = string.Empty;

        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("is_admin")]
        public bool IsAdmin { get; set; }

        [JsonPropertyName("preferred_garden")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Garden? PreferredGarden { get; set; }
    }
}

