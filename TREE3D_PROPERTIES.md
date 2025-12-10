# Tree3D Properties Reference

This document lists all properties that can be set on Tree3D nodes for tree randomization.

## Currently Used Properties

Based on the code in `WorldManager.cs`, these properties are currently being set:

1. **seed** (int)
   - Random seed for tree generation
   - Currently: `plot.PlotUuid.GetHashCode() % 10000`
   - Range: 0 to 9999

2. **trunk_height** (float)
   - Height of the trunk
   - Currently: `baseTrunkHeight * scaleFactor` where `baseTrunkHeight = 5.0f`
   - Units: meters

3. **trunk_max_radius** (float)
   - Maximum radius of the trunk
   - Currently: `baseTrunkMaxRadius * scaleFactor` where `baseTrunkMaxRadius = 0.15f`
   - Units: meters

4. **trunk_length** (float)
   - Length of trunk segments
   - Currently: `baseTrunkLength * scaleFactor` where `baseTrunkLength = 3.0f`
   - Units: meters

5. **twig_scale** (float)
   - Scale of twigs/leaves
   - Currently: `baseTwigScale * scaleFactor * monthTwigMultiplier` where `baseTwigScale = 0.6f`
   - Note: This is affected by seasonal changes (0% to 100% based on month)
   - Units: scale factor

6. **trunk_branches_count** (int)
   - Number of branches on the trunk
   - Currently: Fixed at `3`
   - Range: Typically 0-10+

7. **trunk_branch_length** (float)
   - Length of trunk branches
   - Currently: `baseTrunkBranchLength * scaleFactor` where `baseTrunkBranchLength = 0.7f`
   - Units: meters

8. **material_trunk** (Material)
   - Material for the trunk
   - Currently: Loaded from `res://resources/plot_types/trunk_mat.tres`
   - Type: StandardMaterial3D

9. **material_twig** (Material)
   - Material for twigs/leaves
   - Currently: Loaded from `res://resources/plot_types/twig_mat.tres` with seasonal color modifications
   - Type: StandardMaterial3D

## Additional Tree3D Properties (Likely Available)

Based on typical procedural tree systems, these properties may also be available:

10. **trunk_min_radius** (float)
    - Minimum radius of the trunk (at the top)
    - Units: meters

11. **trunk_taper** (float)
    - How much the trunk tapers from bottom to top
    - Range: 0.0 to 1.0

12. **branch_angle** (float)
    - Angle at which branches grow from trunk
    - Units: radians or degrees

13. **branch_taper** (float)
    - How much branches taper
    - Range: 0.0 to 1.0

14. **twig_count** (int)
    - Number of twigs per branch
    - Range: 0-100+

15. **twig_length** (float)
    - Length of individual twigs
    - Units: meters

16. **twig_angle** (float)
    - Angle at which twigs grow from branches
    - Units: radians or degrees

17. **leaf_count** (int)
    - Number of leaves (if separate from twigs)
    - Range: 0-1000+

18. **leaf_size** (float)
    - Size of leaves
    - Units: meters or scale factor

## JSON Structure Suggestion

For tree randomization, you could provide a JSON structure like this:

```json
{
  "tree_types": [
    {
      "name": "apple_tree",
      "weight": 1.0,
      "trunk_height": 5.0,
      "trunk_max_radius": 0.15,
      "trunk_min_radius": 0.05,
      "trunk_length": 3.0,
      "trunk_taper": 0.8,
      "trunk_branches_count": 3,
      "trunk_branch_length": 0.7,
      "branch_angle": 0.5,
      "branch_taper": 0.6,
      "twig_scale": 0.6,
      "twig_count": 50,
      "twig_length": 0.3,
      "twig_angle": 0.4,
      "leaf_count": 200,
      "leaf_size": 0.1
    },
    {
      "name": "oak_tree",
      "weight": 0.8,
      "trunk_height": 8.0,
      "trunk_max_radius": 0.25,
      "trunk_min_radius": 0.08,
      "trunk_length": 4.0,
      "trunk_taper": 0.7,
      "trunk_branches_count": 5,
      "trunk_branch_length": 1.2,
      "branch_angle": 0.6,
      "branch_taper": 0.5,
      "twig_scale": 0.8,
      "twig_count": 80,
      "twig_length": 0.4,
      "twig_angle": 0.5,
      "leaf_count": 500,
      "leaf_size": 0.15
    }
  ]
}
```

## Notes

- Properties marked with "Currently:" are confirmed to be used in the code
- Properties in "Additional" section are typical for procedural trees but may need verification
- The `weight` field in JSON can be used for weighted random selection
- All float values can have ranges specified (min/max) for randomization within a type
- The `scaleFactor` is currently calculated from plot size, but could also be part of tree type settings

