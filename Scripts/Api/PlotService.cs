#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using GardenManager.Models;

namespace GardenManager.Api
{
	public class PlotService
	{
		private readonly ApiClient _apiClient;

		public PlotService(ApiClient apiClient)
		{
			_apiClient = apiClient;
		}

		/// <summary>
		/// Get full plot details
		/// </summary>
		public async Task<Plot?> GetPlotDetailsAsync(string plotUuid)
		{
			return await _apiClient.GetAsync<Plot>($"/plots/{plotUuid}", true);
		}

		/// <summary>
		/// Get available actions for a plot
		/// </summary>
		public async Task<List<string>?> GetPlotActionsAsync(string plotUuid)
		{
			// TODO: Update endpoint when API is ready
			// For now, return default actions
			return await _apiClient.GetAsync<List<string>>($"/plots/{plotUuid}/actions", true);
		}

		/// <summary>
		/// Execute an action on a plot
		/// </summary>
		public async Task<bool> ExecutePlotActionAsync(string plotUuid, string action, Dictionary<string, object>? parameters = null)
		{
			var body = new Dictionary<string, object>
			{
				{ "action", action }
			};

			if (parameters != null)
			{
				foreach (var param in parameters)
				{
					body[param.Key] = param.Value;
				}
			}

			// TODO: Update endpoint when API is ready
			var result = await _apiClient.PostAsync<Dictionary<string, object>>($"/plots/{plotUuid}/actions", body, true);
			return result != null;
		}

		/// <summary>
		/// Get events for a plot
		/// </summary>
		public async Task<List<Event>?> GetPlotEventsAsync(string plotUuid)
		{
			return await _apiClient.GetAsync<List<Event>>($"/plots/{plotUuid}/events", true);
		}

		/// <summary>
		/// Get rotation-filtered crops for a plot
		/// </summary>
		public async Task<List<Crop>?> GetRotationFilteredCropsAsync(string plotUuid, int year, string? plotGroupUuid = null)
		{
			var endpoint = $"/plots/{plotUuid}/rotation-filtered-crops?year={year}";
			if (!string.IsNullOrEmpty(plotGroupUuid))
			{
				endpoint += $"&plot_group_uuid={plotGroupUuid}";
			}
			return await _apiClient.GetAsync<List<Crop>>(endpoint, true);
		}

		/// <summary>
		/// Get suitable crops for a plot with rotation scores
		/// </summary>
		public async Task<List<CropSuitability>?> GetSuitableCropsAsync(string plotUuid, int year)
		{
			return await _apiClient.GetAsync<List<CropSuitability>>($"/plots/{plotUuid}/suitable-crops?year={year}", true);
		}

		/// <summary>
		/// Plant a crop in a garden
		/// </summary>
		public async Task<Planted?> PlantCropAsync(string gardenUuid, Dictionary<string, object> plantData)
		{
			return await _apiClient.PostAsync<Planted>($"/gardens/{gardenUuid}/crops", plantData, true);
		}
	}
}

