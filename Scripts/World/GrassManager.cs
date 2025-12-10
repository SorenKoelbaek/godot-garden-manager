using Godot;
using System.Collections.Generic;

public partial class GrassManager : MultiMeshInstance3D
{
    private MultiMesh _multiMesh;
    private Mesh _grassBladeMesh;
    
    // Grass parameters matching the repository
    [Export]
    public Vector2 BladeWidth { get; set; } = new Vector2(0.01f, 0.02f);
    
    [Export]
    public Vector2 BladeHeight { get; set; } = new Vector2(0.04f, 0.08f);
    
    [Export]
    public Vector2 SwayYaw { get; set; } = new Vector2(0.0f, 10.0f);
    
    [Export]
    public Vector2 SwayPitch { get; set; } = new Vector2(0.04f, 0.08f);
    
    [Export]
    public float Density { get; set; } = 1.0f;
    
    [Export]
    public Mesh SourceMesh { get; set; } = null;
    
    public override void _Ready()
    {
        GD.Print("GrassManager: Initializing BotW-style grass system");
        
        // Create the simple triangle grass blade mesh
        _grassBladeMesh = CreateGrassBladeMesh();
        
        // Create multi-mesh for efficient rendering
        _multiMesh = new MultiMesh();
        Multimesh = _multiMesh;
        _multiMesh.Mesh = _grassBladeMesh;
        _multiMesh.InstanceCount = 0;
        _multiMesh.TransformFormat = MultiMesh.TransformFormatEnum.Transform3D;
        _multiMesh.UseCustomData = true;
        _multiMesh.UseColors = false;
        
        // Load material from .tres file
        var grassMaterial = GD.Load<Material>("res://resources/grass/mat_grass.tres");
        if (grassMaterial != null)
        {
            MaterialOverride = grassMaterial;
            GD.Print("GrassManager: Loaded grass material");
        }
        else
        {
            GD.PrintErr("GrassManager: Failed to load grass material!");
        }
        
        GD.Print("GrassManager: Grass system initialized");
    }
    
    private Mesh CreateGrassBladeMesh()
    {
        // Create simple triangle mesh exactly like mesh_factory.gd simple_grass
        var verts = new Vector3[]
        {
            new Vector3(-0.5f, 0.0f, 0.0f),  // bottom-left
            new Vector3(0.5f, 0.0f, 0.0f),   // bottom-right
            new Vector3(0.0f, 1.0f, 0.0f)    // top-center
        };
        
        var uvs = new Vector2[]
        {
            new Vector2(0.0f, 0.0f),  // bottom-left
            new Vector2(0.0f, 0.0f),  // bottom-right
            new Vector2(1.0f, 1.0f)   // top-center
        };
        
        var surfaceArray = new Godot.Collections.Array();
        surfaceArray.Resize((int)Mesh.ArrayType.Max);
        surfaceArray[(int)Mesh.ArrayType.Vertex] = verts;
        surfaceArray[(int)Mesh.ArrayType.TexUV2] = uvs; // UV2 is used for the shader
        
        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surfaceArray);
        mesh.CustomAabb = new Aabb(new Vector3(-0.5f, 0.0f, -0.5f), new Vector3(1.0f, 1.0f, 1.0f));
        
        return mesh;
    }
    
    public void GenerateGrassForMesh(Mesh mesh)
    {
        SourceMesh = mesh;
        Rebuild();
    }
    
    private void Rebuild()
    {
        if (_multiMesh == null)
        {
            _multiMesh = new MultiMesh();
        }
        
        if (SourceMesh == null)
        {
            GD.PrintErr("GrassManager: No source mesh provided!");
            return;
        }
        
        _multiMesh.InstanceCount = 0;
        
        var spawns = GenerateGrassSpawns(
            SourceMesh,
            Density,
            BladeWidth,
            BladeHeight,
            SwayPitch,
            SwayYaw
        );
        
        if (spawns.Count == 0)
        {
            GD.Print("GrassManager: No grass spawns generated");
            return;
        }
        
        _multiMesh.Mesh = _grassBladeMesh;
        _multiMesh.TransformFormat = MultiMesh.TransformFormatEnum.Transform3D;
        _multiMesh.UseCustomData = true;
        _multiMesh.UseColors = false;
        _multiMesh.InstanceCount = spawns.Count;
        
        for (int i = 0; i < spawns.Count; i++)
        {
            var spawn = spawns[i];
            _multiMesh.SetInstanceTransform(i, spawn.Transform);
            _multiMesh.SetInstanceCustomData(i, spawn.CustomData);
        }
        
        GD.Print($"GrassManager: Generated {spawns.Count} grass blades");
    }
    
    private struct GrassSpawn
    {
        public Transform3D Transform;
        public Color CustomData; // (width, height, pitch, yaw)
    }
    
    private List<GrassSpawn> GenerateGrassSpawns(
        Mesh mesh,
        float density,
        Vector2 bladeWidth,
        Vector2 bladeHeight,
        Vector2 swayPitch,
        Vector2 swayYaw)
    {
        var spawns = new List<GrassSpawn>();
        
        if (mesh == null)
        {
            return spawns;
        }
        
        // Get mesh surface arrays - exactly like grass_factory.gd
        var surfaceArrays = mesh.SurfaceGetArrays(0);
        var indicesVariant = surfaceArrays[(int)Mesh.ArrayType.Index];
        var positionsVariant = surfaceArrays[(int)Mesh.ArrayType.Vertex];
        var normalsVariant = surfaceArrays[(int)Mesh.ArrayType.Normal];
        
        if (indicesVariant.VariantType != Variant.Type.PackedInt32Array ||
            positionsVariant.VariantType != Variant.Type.PackedVector3Array ||
            normalsVariant.VariantType != Variant.Type.PackedVector3Array)
        {
            GD.PrintErr($"GrassManager: Invalid mesh data! Types: {indicesVariant.VariantType}, {positionsVariant.VariantType}, {normalsVariant.VariantType}");
            return spawns;
        }
        
        // In Godot 4.5 C#, we can use Variant's implicit conversion or AsGodotObject
        // Try to get the arrays directly - use the Variant's underlying object
        var indicesObj = indicesVariant.AsGodotObject();
        var positionsObj = positionsVariant.AsGodotObject();
        var normalsObj = normalsVariant.AsGodotObject();
        
        // Get array sizes
        int indexCount = (int)indicesObj.Call("size");
        int positionCount = (int)positionsObj.Call("size");
        int normalCount = (int)normalsObj.Call("size");
        
        GD.Print($"GrassManager: Mesh has {indexCount} indices, {positionCount} positions, {normalCount} normals");
        
        // Convert to C# lists by iterating through arrays
        var indices = new List<int>();
        var positions = new List<Vector3>();
        var normals = new List<Vector3>();
        
        for (int i = 0; i < indexCount; i++)
        {
            var indexVal = indicesObj.Call("get", i);
            indices.Add((int)indexVal);
        }
        
        for (int i = 0; i < positionCount; i++)
        {
            var posVal = positionsObj.Call("get", i);
            positions.Add((Vector3)posVal);
        }
        
        for (int i = 0; i < normalCount; i++)
        {
            var normVal = normalsObj.Call("get", i);
            normals.Add((Vector3)normVal);
        }
        
        GD.Print($"GrassManager: Converted arrays - {indices.Count} indices, {positions.Count} positions, {normals.Count} normals");
        
        var random = new System.Random();
        
        // Process each triangle - exactly like grass_factory.gd
        GD.Print($"GrassManager: Processing triangles, indexCount={indexCount}");
        for (int i = 0; i < indexCount; i += 3)
        {
            if (i + 2 >= indices.Count)
            {
                GD.PrintErr($"GrassManager: Index out of range: i={i}, indices.Count={indices.Count}");
                break;
            }
            
            int j = indices[i];
            int k = indices[i + 1];
            int l = indices[i + 2];
            
            if (j >= positions.Count || k >= positions.Count || l >= positions.Count)
            {
                GD.PrintErr($"GrassManager: Position index out of range: j={j}, k={k}, l={l}, positions.Count={positions.Count}");
                continue;
            }
            
            // Calculate triangle area
            float area = TriangleArea(positions[j], positions[k], positions[l]);
            
            // Calculate number of blades for this triangle
            int bladesPerFace = (int)Mathf.Round(area * density);
            
            // Generate grass blades on this triangle
            for (int bladeIndex = 0; bladeIndex < bladesPerFace; bladeIndex++)
            {
                // Random barycentric coordinates
                Vector3 uvw = RandomBarycentric(random);
                
                // Calculate position on triangle
                Vector3 position = FromBarycentricVector3(uvw, positions[j], positions[k], positions[l]);
                
                // Calculate normal on triangle
                Vector3 normal = FromBarycentricVector3(uvw, normals[j], normals[k], normals[l]).Normalized();
                
                // Random rotation around Y axis
                Quaternion q1 = Quaternion.FromEuler(new Vector3(0, Mathf.DegToRad((float)(random.NextDouble() * 360.0)), 0));
                
                // Rotate to align with triangle normal
                Quaternion q2 = QuatShortestArc(Vector3.Up, normal);
                
                // Combine rotations
                Basis basis = new Basis(q2 * q1);
                Transform3D transform = new Transform3D(basis, position);
                
                // Random blade parameters - exactly like grass_factory.gd
                float widthParam = (float)(random.NextDouble() * (bladeWidth.Y - bladeWidth.X) + bladeWidth.X);
                float heightParam = (float)(random.NextDouble() * (bladeHeight.Y - bladeHeight.X) + bladeHeight.X);
                float pitchParam = Mathf.DegToRad((float)(random.NextDouble() * (swayPitch.Y - swayPitch.X) + swayPitch.X));
                float yawParam = Mathf.DegToRad((float)(random.NextDouble() * (swayYaw.Y - swayYaw.X) + swayYaw.X));
                
                // Custom data: (width, height, pitch, yaw)
                Color customData = new Color(widthParam, heightParam, pitchParam, yawParam);
                
                spawns.Add(new GrassSpawn
                {
                    Transform = transform,
                    CustomData = customData
                });
            }
        }
        
        return spawns;
    }
    
    private Vector3 RandomBarycentric(System.Random random)
    {
        float u = (float)random.NextDouble();
        float v = (float)random.NextDouble();
        if (u + v >= 1.0f)
        {
            u = 1.0f - u;
            v = 1.0f - v;
        }
        return new Vector3(u, v, 1.0f - (u + v));
    }
    
    private Vector3 FromBarycentricVector3(Vector3 uvw, Vector3 a, Vector3 b, Vector3 c)
    {
        return (a * uvw.X) + (b * uvw.Y) + (c * uvw.Z);
    }
    
    private Quaternion QuatShortestArc(Vector3 normalFrom, Vector3 normalTo)
    {
        float dot = normalFrom.Dot(normalTo);
        if (dot > 0.999999f)
        {
            return Quaternion.Identity;
        }
        if (dot < -0.999999f)
        {
            Vector3 orthogonal = GetOrthogonalTo(normalFrom);
            return new Quaternion(orthogonal, Mathf.Pi);
        }
        Vector3 axis = normalFrom.Cross(normalTo);
        return new Quaternion(axis.X, axis.Y, axis.Z, 1.0f + dot).Normalized();
    }
    
    private Vector3 GetOrthogonalTo(Vector3 v)
    {
        float x = Mathf.Abs(v.X);
        float y = Mathf.Abs(v.Y);
        float z = Mathf.Abs(v.Z);
        Vector3 other = Vector3.Forward;
        if (x > y && x > z)
        {
            other = Vector3.Right;
        }
        else if (y > z)
        {
            other = Vector3.Up;
        }
        return v.Cross(other);
    }
    
    private float TriangleArea(Vector3 a, Vector3 b, Vector3 c)
    {
        float distA = a.DistanceTo(b);
        float distB = b.DistanceTo(c);
        float distC = c.DistanceTo(a);
        float s = (distA + distB + distC) / 2.0f;
        return Mathf.Sqrt(s * (s - distA) * (s - distB) * (s - distC));
    }
}
