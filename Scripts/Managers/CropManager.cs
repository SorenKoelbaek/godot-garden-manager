#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GardenManager.Api;
using GardenManager.Models;
using Godot;
using Serilog;

public partial class CropManager : Node
{
	private List<Crop> _allCrops = new List<Crop>();
	private bool _isLoaded = false;
	private ApiClient? _apiClient;

	public List<Crop> AllCrops => _allCrops;
	public bool IsLoaded => _isLoaded;

	public override void _Ready()
	{
		Log.Debug("CropManager: _Ready() called");
		_apiClient = GetNodeOrNull<ApiClient>("/root/ApiClient");
	}

	/// <summary>
	/// Load all crops from the API
	/// </summary>
	public async Task LoadAllCropsAsync()
	{
		if (_isLoaded)
		{
			Log.Debug("CropManager: Crops already loaded, skipping");
			return;
		}

		if (_apiClient == null)
		{
			Log.Error("CropManager: ApiClient not found!");
			return;
		}

		Log.Debug("CropManager: Loading all crops from API");
		var crops = await _apiClient.GetAsync<List<Crop>>("/crops", true);
		
		if (crops != null)
		{
			_allCrops = crops;
			_isLoaded = true;
			Log.Information("CropManager: Loaded {Count} crops", _allCrops.Count);
		}
		else
		{
			Log.Warning("CropManager: Failed to load crops from API");
		}
	}

	/// <summary>
	/// Get rotation-filtered crops for a plot (filters from cached crops)
	/// </summary>
	public List<Crop> GetRotationFilteredCrops(string plotUuid, int year, string? plotGroupUuid = null)
	{
		if (!_isLoaded)
		{
			Log.Warning("CropManager: Crops not loaded yet, returning empty list");
			return new List<Crop>();
		}

		// For now, return all crops. In the future, this could filter based on rotation rules
		// The API endpoint /plots/{plot_uuid}/rotation-filtered-crops should be used for actual filtering
		// but we can cache the full list and do client-side filtering if needed
		return _allCrops.ToList();
	}

	/// <summary>
	/// Clear the cache (useful for testing or reloading)
	/// </summary>
	public void ClearCache()
	{
		_allCrops.Clear();
		_isLoaded = false;
		Log.Debug("CropManager: Cache cleared");
	}
}

