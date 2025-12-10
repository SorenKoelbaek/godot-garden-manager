using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using GardenManager.Models;
using Godot;

public static class TreeTypeLoader
{
    private static TreeTypeData? _cachedTreeTypes;
    private static readonly string TreeTypesPath = "res://resources/tree/tree_types.json";

    public static TreeTypeData LoadTreeTypes()
    {
        if (_cachedTreeTypes != null)
        {
            return _cachedTreeTypes;
        }

        var file = Godot.FileAccess.Open(TreeTypesPath, Godot.FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PrintErr($"TreeTypeLoader: Failed to open tree types file: {TreeTypesPath}");
            return new TreeTypeData();
        }

        string jsonText = file.GetAsText();
        file.Close();

        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            _cachedTreeTypes = JsonSerializer.Deserialize<TreeTypeData>(jsonText, options);
            
            if (_cachedTreeTypes == null)
            {
                GD.PrintErr("TreeTypeLoader: Failed to deserialize tree types");
                return new TreeTypeData();
            }

            GD.Print($"TreeTypeLoader: Loaded {_cachedTreeTypes.TreeTypes.Count} tree types");
            return _cachedTreeTypes;
        }
        catch (JsonException ex)
        {
            GD.PrintErr($"TreeTypeLoader: JSON deserialization error: {ex.Message}");
            return new TreeTypeData();
        }
    }

    public static TreeType? GetRandomTreeType()
    {
        var treeTypes = LoadTreeTypes();
        if (treeTypes.TreeTypes.Count == 0)
        {
            return null;
        }

        // Weighted random selection
        float totalWeight = treeTypes.TreeTypes.Sum(t => t.Weight);
        if (totalWeight <= 0)
        {
            return treeTypes.TreeTypes[0]; // Fallback to first tree
        }

        float randomValue = GD.Randf() * totalWeight;
        float currentWeight = 0.0f;

        foreach (var treeType in treeTypes.TreeTypes)
        {
            currentWeight += treeType.Weight;
            if (randomValue <= currentWeight)
            {
                return treeType;
            }
        }

        // Fallback to last tree (shouldn't happen)
        return treeTypes.TreeTypes[treeTypes.TreeTypes.Count - 1];
    }
}

