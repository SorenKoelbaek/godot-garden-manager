using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GardenManager.Models
{
    public class TreeTypeData
    {
        [JsonPropertyName("tree_types")]
        public List<TreeType> TreeTypes { get; set; } = new List<TreeType>();
    }

    public class TreeType
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("weight")]
        public float Weight { get; set; } = 1.0f;

        [JsonPropertyName("seed")]
        public int? Seed { get; set; }

        [JsonPropertyName("trunk_height")]
        public float? TrunkHeight { get; set; }

        [JsonPropertyName("trunk_branches_count")]
        public int? TrunkBranchesCount { get; set; }

        [JsonPropertyName("trunk_branch_length")]
        public float? TrunkBranchLength { get; set; }

        [JsonPropertyName("trunk_branch_length_falloff")]
        public float? TrunkBranchLengthFalloff { get; set; }

        [JsonPropertyName("trunk_max_radius")]
        public float? TrunkMaxRadius { get; set; }

        [JsonPropertyName("trunk_radius_falloff_rate")]
        public float? TrunkRadiusFalloffRate { get; set; }

        [JsonPropertyName("trunk_length")]
        public float? TrunkLength { get; set; }

        [JsonPropertyName("trunk_twist")]
        public float? TrunkTwist { get; set; }

        [JsonPropertyName("trunk_kink")]
        public float? TrunkKink { get; set; }

        [JsonPropertyName("trunk_climb_rate")]
        public float? TrunkClimbRate { get; set; }

        [JsonPropertyName("trunk_drop_amount")]
        public float? TrunkDropAmount { get; set; }

        [JsonPropertyName("trunk_grow_amount")]
        public float? TrunkGrowAmount { get; set; }

        [JsonPropertyName("twig_scale")]
        public float? TwigScale { get; set; }

        [JsonPropertyName("material_trunk")]
        public string? MaterialTrunk { get; set; }

        [JsonPropertyName("material_twig")]
        public string? MaterialTwig { get; set; }
    }
}

