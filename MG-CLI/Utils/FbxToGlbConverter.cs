using System.Numerics;
using Assimp;
using Assimp.Configs;
using SharpGLTF.Geometry;
using SharpGLTF.Materials;
using SharpGLTF.Scenes;
using SharpGLTF.Schema2;
using Spectre.Console;
using Matrix4x4 = System.Numerics.Matrix4x4;
using Mesh = Assimp.Mesh;
using Node = Assimp.Node;
using Scene = Assimp.Scene;

// Convenience aliases for vertex types
using VPOSNORM = SharpGLTF.Geometry.VertexTypes.VertexPositionNormal;
using VTEX1 = SharpGLTF.Geometry.VertexTypes.VertexTexture1;
using VJOINTS = SharpGLTF.Geometry.VertexTypes.VertexJoints4;
using VERTEX = SharpGLTF.Geometry.VertexBuilder<SharpGLTF.Geometry.VertexTypes.VertexPositionNormal, SharpGLTF.Geometry.VertexTypes.VertexTexture1, SharpGLTF.Geometry.VertexTypes.VertexJoints4>;

namespace MG_CLI;

public static class FbxToGlbConverter
{
    // Public entry point
    public static ModelRoot ExportFbxToGlb(string fbxPath, string glbPath)
    {
        if (!File.Exists(fbxPath))
            throw new FileNotFoundException("FBX file not found", fbxPath);

        Log.Print($"Converting FBX to GLB\n  from: {fbxPath}\n  to: {glbPath}");
        
        var config = new FBXPreservePivotsConfig(false);

        using var context = new AssimpContext();
        context.SetConfig(config);

        // Load FBX with everything we need for skinning
        const PostProcessSteps postProcessFlags = PostProcessSteps.Triangulate
                                                  | PostProcessSteps.LimitBoneWeights
                                                  | PostProcessSteps.JoinIdenticalVertices
                                                  | PostProcessSteps.ImproveCacheLocality
                                                  | PostProcessSteps.GenerateNormals; 
                                                  // | PostProcessSteps.ValidateDataStructure;

        var scene = context.ImportFile(fbxPath, postProcessFlags);
        if (scene == null || scene.MeshCount == 0)
            throw new InvalidOperationException("[mixamo:export] Scene has no meshes.");

        // Collect all bones and map them to indices
        var bones = CollectBones(scene);

        // Build SharpGLTF node hierarchy from Assimp nodes
        var nodeMap = BuildNodeHierarchy(scene, bones);

        // Create SceneBuilder and add skinned mesh
        var sceneBuilder = new SceneBuilder();

        AddSkinnedMeshWithIbms(scene, sceneBuilder, bones, nodeMap);        

        // Bake to glTF2 model and save as .glb
        var model = sceneBuilder.ToGltf2();
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(glbPath))!);
        model.SaveGLB(glbPath);
        
        Log.Print($"Completed: {glbPath}", Color.Green);
        return model;
    }
    
    private static void AddSkinnedMeshWithIbms(
        Scene scene,
        SceneBuilder sceneBuilder,
        Dictionary<string, BoneInfo> bones,
        Dictionary<string, NodeBuilder> nodeMap)
    {
        var jointNodes = new List<NodeBuilder>();
        var ibmList   = new List<Matrix4x4>();

        foreach (var kvp in bones)
        {
            var boneInfo = kvp.Value;
            var boneName = boneInfo.Name;

            if (!nodeMap.TryGetValue(boneName, out var jointNode))
                continue; // skip bones that don't have a corresponding node

            jointNodes.Add(jointNode);

            // Assimp's OffsetMatrix: mesh space -> bone space in bind pose
            // which is exactly what glTF expects as an inverseBindMatrix.
            var aMat = boneInfo.Bone.OffsetMatrix;
            var m = new Matrix4x4(
                aMat.A1, aMat.B1, aMat.C1, aMat.D1,
                aMat.A2, aMat.B2, aMat.C2, aMat.D2,
                aMat.A3, aMat.B3, aMat.C3, aMat.D3,
                aMat.A4, aMat.B4, aMat.C4, aMat.D4)
            {
                // Make sure bottom-right is 1 (numeric noise guard)
                M44 = 1
            };

            ibmList.Add(m);
        }

        var jointTuples = jointNodes
            .Zip(ibmList, (joint, ibm) => (Joint: joint, InverseBindMatrix: ibm))
            .ToArray();

        var sceneNames = scene.BuildMeshIndexToNodeNameMap();
        
        for (int mi = 0; mi < scene.MeshCount; mi++)
        {
            var assimpMesh = scene.Meshes[mi];
            assimpMesh.Name = sceneNames[mi];

            // Build one GLTF mesh per Assimp mesh
            var meshBuilder = BuildSkinnedMeshSingle(assimpMesh, bones);

            // Give it a stable node name so Godot imports separate children like Body01, Head01, etc.
            var meshName = string.IsNullOrWhiteSpace(assimpMesh.Name) ? $"Mesh_{mi:00}" : assimpMesh.Name;
            
            // This names the glTF node instance (important for Godot)
            var skin = sceneBuilder.AddSkinnedMesh(meshBuilder, jointTuples);
            skin?.WithName(meshName);
            Log.Print($"SkinName: {skin?.Name}");
        }
    }
    
    private static MeshBuilder<VPOSNORM, VTEX1, VJOINTS> BuildSkinnedMeshSingle(
        Mesh mesh,
        Dictionary<string, BoneInfo> bones)
    {
        var meshName = string.IsNullOrWhiteSpace(mesh.Name) ? "mesh" : mesh.Name;

        var meshBuilder = new MeshBuilder<VPOSNORM, VTEX1, VJOINTS>(meshName);

        var material = new MaterialBuilder()
            .WithDoubleSide(true)
            .WithMetallicRoughnessShader();

        var prim = meshBuilder.UsePrimitive(material);

        var vertexCount = mesh.VertexCount;

        var boneIndices = new Vector4[vertexCount];
        var boneWeights = new Vector4[vertexCount];

        // Same weight packing logic you already have:
        foreach (var assimpBone in mesh.Bones)
        {
            if (!bones.TryGetValue(assimpBone.Name, out var boneInfo))
                continue;

            var jointIndex = boneInfo.Index;

            foreach (var vw in assimpBone.VertexWeights)
            {
                var vId = vw.VertexID;
                var w = vw.Weight;

                ref var idx = ref boneIndices[vId];
                ref var wt = ref boneWeights[vId];

                if (wt.X == 0)
                {
                    idx.X = jointIndex;
                    wt.X = w;
                }
                else if (wt.Y == 0)
                {
                    idx.Y = jointIndex;
                    wt.Y = w;
                }
                else if (wt.Z == 0)
                {
                    idx.Z = jointIndex;
                    wt.Z = w;
                }
                else if (wt.W == 0)
                {
                    idx.W = jointIndex;
                    wt.W = w;
                }
            }
        }

        // Normalize weights
        for (int i = 0; i < vertexCount; i++)
        {
            var w = boneWeights[i];
            var sum = w.X + w.Y + w.Z + w.W;
            if (sum > 0)
            {
                var inv = 1f / sum;
                boneWeights[i] = new Vector4(w.X * inv, w.Y * inv, w.Z * inv, w.W * inv);
            }
        }

        // Triangles
        for (int f = 0; f < mesh.FaceCount; f++)
        {
            var face = mesh.Faces[f];
            if (face.IndexCount != 3) continue;

            var v0 = CreateVertex(mesh, face.Indices[0], boneIndices, boneWeights);
            var v1 = CreateVertex(mesh, face.Indices[1], boneIndices, boneWeights);
            var v2 = CreateVertex(mesh, face.Indices[2], boneIndices, boneWeights);

            prim.AddTriangle(v0, v1, v2);
        }

        return meshBuilder;
    }


    #region Bone / skeleton helpers

    private sealed class BoneInfo
    {
        public string Name = string.Empty;
        public int Index;
        public Bone Bone;
    }

    private static Dictionary<string, BoneInfo> CollectBones(Scene scene)
    {
        var bones = new Dictionary<string, BoneInfo>();

        foreach (var mesh in scene.Meshes)
        {
            foreach (var bone in mesh.Bones)
            {
                if (!bones.TryGetValue(bone.Name, out var info))
                {
                    info = new BoneInfo
                    {
                        Name = bone.Name,
                        Index = bones.Count,
                        Bone = bone
                    };

                    bones.Add(bone.Name, info);
                }
                else
                {
                    // If same bone name appears in multiple meshes, trust the first offset.
                    // You *could* assert same matrix here if you want to be strict.
                }
            }
        }

        return bones;
    }

    private static Dictionary<string, NodeBuilder> BuildNodeHierarchy(
        Scene scene,
        Dictionary<string, BoneInfo> bones)
    {
        var nodeMap = new Dictionary<string, NodeBuilder>();

        // Local variable that the local function can close over
        NodeBuilder? armatureRootLocal = null;

        // Build hierarchy starting at scene root
        BuildNode(scene.RootNode, null);

        return nodeMap;

        // Recursive local function to mirror Assimp's node tree into NodeBuilder hierarchy
        void BuildNode(Node assimpNode, NodeBuilder? parent)
        {
            var localTransform = ToNumerics(assimpNode.Transform);

            var nodeBuilder = new NodeBuilder(assimpNode.Name)
            {
                LocalTransform = localTransform
            };

            parent?.AddNode(nodeBuilder);

            nodeMap[assimpNode.Name] = nodeBuilder;

            // Heuristic: first bone node we encounter becomes our armature root
            if (bones.ContainsKey(assimpNode.Name) && armatureRootLocal == null)
                armatureRootLocal = nodeBuilder;

            foreach (var child in assimpNode.Children)
                BuildNode(child, nodeBuilder);
        }
    }

    #endregion

    #region Mesh building

    private static MeshBuilder<VPOSNORM, VTEX1, VJOINTS> BuildSkinnedMesh(
        Scene scene,
        Dictionary<string, BoneInfo> bones,
        Dictionary<string, NodeBuilder> nodeMap)
    {
        var meshBuilder = new MeshBuilder<VPOSNORM, VTEX1, VJOINTS>("mesh");

        // Simple single material; you can extend this to read materials from Assimp
        var material = new MaterialBuilder()
            .WithDoubleSide(true)
            .WithMetallicRoughnessShader();

        var prim = meshBuilder.UsePrimitive(material);

        // We'll merge all Assimp meshes into one meshBuilder.
        // To keep vertex indices separate, we just append sequentially.
        foreach (var mesh in scene.Meshes)
        {
            var vertexCount = mesh.VertexCount;

            // Compute per-vertex bone weights & indices
            var boneIndices = new Vector4[vertexCount];
            var boneWeights = new Vector4[vertexCount];

            foreach (var assimpBone in mesh.Bones)
            {
                if (!bones.TryGetValue(assimpBone.Name, out var boneInfo))
                    continue;

                var jointIndex = boneInfo.Index;

                foreach (var vw in assimpBone.VertexWeights)
                {
                    var vId = vw.VertexID;
                    var w = vw.Weight;

                    // Pack into first free slot (max 4)
                    ref var idx = ref boneIndices[vId];
                    ref var wt = ref boneWeights[vId];

                    if (wt.X == 0) { idx.X = jointIndex; wt.X = w; }
                    else if (wt.Y == 0) { idx.Y = jointIndex; wt.Y = w; }
                    else if (wt.Z == 0) { idx.Z = jointIndex; wt.Z = w; }
                    else if (wt.W == 0) { idx.W = jointIndex; wt.W = w; }
                    else
                    {
                        // Already have 4 weights, you can either skip or replace the smallest.
                        // Simple version: ignore extra influences.
                    }
                }
            }

            // Normalise weights so each vertex sums to 1 (if any weight present)
            for (int i = 0; i < vertexCount; i++)
            {
                var w = boneWeights[i];
                var sum = w.X + w.Y + w.Z + w.W;
                if (sum > 0)
                {
                    var inv = 1.0f / sum;
                    boneWeights[i] = new Vector4(w.X * inv, w.Y * inv, w.Z * inv, w.W * inv);
                }
            }

            // Build vertices and triangles
            for (int f = 0; f < mesh.FaceCount; f++)
            {
                var face = mesh.Faces[f];
                if (face.IndexCount != 3) continue; // triangulated, so should always be 3

                VERTEX v0 = CreateVertex(mesh, face.Indices[0], boneIndices, boneWeights);
                VERTEX v1 = CreateVertex(mesh, face.Indices[1], boneIndices, boneWeights);
                VERTEX v2 = CreateVertex(mesh, face.Indices[2], boneIndices, boneWeights);

                prim.AddTriangle(v0, v1, v2);
            }
        }

        return meshBuilder;
    }

    private static VERTEX CreateVertex(
        Mesh mesh,
        int index,
        Vector4[] boneIndices,
        Vector4[] boneWeights)
    {
        var pos = mesh.HasVertices ? mesh.Vertices[index] : new Vector3D();
        var nrm = mesh.HasNormals ? mesh.Normals[index] : new Vector3D(0, 1, 0);

        Vector3D uv = default;
        if (mesh.TextureCoordinateChannelCount > 0 &&
            mesh.TextureCoordinateChannels[0].Count > index)
        {
            uv = mesh.TextureCoordinateChannels[0][index];
        }

        var vPosNorm = new VPOSNORM(
            pos.X, pos.Y, pos.Z,
            nrm.X, nrm.Y, nrm.Z);

        var vTex = new VTEX1(new Vector2(uv.X, uv.Y));

        var bi = boneIndices[index];
        var bw = boneWeights[index];

        var j0 = ((int)bi.X, bw.X);
        var j1 = ((int)bi.Y, bw.Y);
        var j2 = ((int)bi.Z, bw.Z);
        var j3 = ((int)bi.W, bw.W);

        var vJoints = new VJOINTS(j0, j1, j2, j3);

        return new VERTEX(vPosNorm, vTex, vJoints);
    }

    #endregion

    #region Matrix conversions

    private static Matrix4x4 ToNumerics(Assimp.Matrix4x4 assimpMatrix)
    {
        // Assimp's Matrix4x4 has same layout as System.Numerics.Matrix4x4 (row-major),
        // but types are different, so we map field by field.
        return new Matrix4x4(
            assimpMatrix.A1, assimpMatrix.B1, assimpMatrix.C1, assimpMatrix.D1,
            assimpMatrix.A2, assimpMatrix.B2, assimpMatrix.C2, assimpMatrix.D2,
            assimpMatrix.A3, assimpMatrix.B3, assimpMatrix.C3, assimpMatrix.D3,
            assimpMatrix.A4, assimpMatrix.B4, assimpMatrix.C4, assimpMatrix.D4
        );
    }

    #endregion
}
