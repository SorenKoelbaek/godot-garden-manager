#nullable enable
using System.Text;
using System.Text.Json;
using GardenManager.Models;
using Godot;

namespace GardenManager.Auth
{
    public class CredentialManager
    {
        private const string CredentialsPath = "user://credentials.json";
        private const string SettingsPath = "user://settings.json";

        public Credentials? LoadCredentials()
        {
            if (!FileAccess.FileExists(CredentialsPath))
            {
                return null;
            }

            using var file = FileAccess.Open(CredentialsPath, FileAccess.ModeFlags.Read);
            if (file == null)
            {
                return null;
            }

            var jsonString = file.GetAsText();
            if (string.IsNullOrEmpty(jsonString))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<Credentials>(jsonString);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        public void SaveCredentials(Credentials credentials)
        {
            var jsonString = JsonSerializer.Serialize(credentials, new JsonSerializerOptions
            {
                WriteIndented = false
            });

            using var file = FileAccess.Open(CredentialsPath, FileAccess.ModeFlags.Write);
            if (file != null)
            {
                file.StoreString(jsonString);
            }
        }

        public void ClearCredentials()
        {
            if (FileAccess.FileExists(CredentialsPath))
            {
                DirAccess.RemoveAbsolute(CredentialsPath);
            }
        }

        public GameSettings? LoadSettings()
        {
            if (!FileAccess.FileExists(SettingsPath))
            {
                return null;
            }

            using var file = FileAccess.Open(SettingsPath, FileAccess.ModeFlags.Read);
            if (file == null)
            {
                return null;
            }

            var jsonString = file.GetAsText();
            if (string.IsNullOrEmpty(jsonString))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<GameSettings>(jsonString);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        public void SaveSettings(GameSettings settings)
        {
            var jsonString = JsonSerializer.Serialize(settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            using var file = FileAccess.Open(SettingsPath, FileAccess.ModeFlags.Write);
            if (file != null)
            {
                file.StoreString(jsonString);
            }
        }
    }
}


