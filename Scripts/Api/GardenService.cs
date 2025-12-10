#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GardenManager.Api;
using GardenManager.Models;

namespace GardenManager.Api
{
    public class GardenService
    {
        private readonly ApiClient _apiClient;

        public GardenService(ApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<User?> GetCurrentUserAsync()
        {
            return await _apiClient.GetAsync<User>("/me", true);
        }

        public async Task<List<GardenManager.Models.Garden>> GetGardensAsync()
        {
            var gardens = await _apiClient.GetAsync<List<GardenManager.Models.Garden>>("/gardens", true);
            return gardens ?? new List<GardenManager.Models.Garden>();
        }

        public async Task<GardenManager.Models.Garden?> GetGardenAsync(string gardenUuid)
        {
            return await _apiClient.GetAsync<GardenManager.Models.Garden>($"/gardens/{gardenUuid}", true);
        }

        public async Task<List<Plot>> GetPlotsAsync(string gardenUuid)
        {
            var plots = await _apiClient.GetAsync<List<Plot>>($"/gardens/{gardenUuid}/plots", true);
            return plots ?? new List<Plot>();
        }
    }
}

