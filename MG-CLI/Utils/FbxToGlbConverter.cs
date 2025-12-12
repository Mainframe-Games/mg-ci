using System.Numerics;
using Assimp;
using Assimp.Configs;
using SharpGLTF.Geometry;
using SharpGLTF.Materials;
using SharpGLTF.Scenes;

// Convenience aliases for vertex types
using VPOSNORM = SharpGLTF.Geometry.VertexTypes.VertexPositionNormal;
using VTEX1 = SharpGLTF.Geometry.VertexTypes.VertexTexture1;
using VJOINTS = SharpGLTF.Geometry.VertexTypes.VertexJoints4;
using VERTEX = SharpGLTF.Geometry.VertexBuilder<SharpGLTF.Geometry.VertexTypes.VertexPositionNormal, SharpGLTF.Geometry.VertexTypes.VertexTexture1, SharpGLTF.Geometry.VertexTypes.VertexJoints4>;

namespace MG_CLI;

public static class FbxToGlbConverter
{
    // Public entry point
    public static void ExportFbxToGlb(string fbxPath, string glbPath)
    {
        if (!File.Exists(fbxPath))
            throw new FileNotFoundException("FBX file not found", fbxPath);

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

        // 1) Collect all bones and map them to indices
        var bones = CollectBones(scene);

        // 2) Build SharpGLTF node hierarchy from Assimp nodes
        var nodeMap = BuildNodeHierarchy(scene, bones, out var armatureRoot);

        // 3) Build a single skinned MeshBuilder from all meshes
        var meshBuilder = BuildSkinnedMesh(scene, bones, nodeMap);

        // 4) Create SceneBuilder and add skinned mesh
        var sceneBuilder = new SceneBuilder();

        // Armature root defaults to scene root if we didn't find a better one
        var rootNodeBuilder = armatureRoot ?? nodeMap[scene.RootNode.Name];

        // We pass *all* joint nodes that correspond to bones
        var jointNodes = bones.Values
            .Where(b => b.Node != null && nodeMap.TryGetValue(b.Node.Name, out _))
            .Select(b => nodeMap[b.Node.Name])
            .Distinct()
            .ToArray();

        // NOTE: The signature of AddSkinnedMesh in Toolkit is:
        // InstanceBuilder AddSkinnedMesh(
        //     MeshBuilder<VertexPositionNormal, VertexTexture1, VertexJoints4> mesh,
        //     Matrix4x4 localTransform,
        //     NodeBuilder armatureRoot,
        //     params NodeBuilder[] joints);
        //
        // If your intellisense shows a slightly different overload,
        // adjust the call accordingly – concept is the same.
        var nodes = new[] { rootNodeBuilder }.Union(jointNodes).ToArray();
        sceneBuilder.AddSkinnedMesh(
            meshBuilder,
            System.Numerics.Matrix4x4.Identity,
            nodes);

        // 5) Bake to glTF2 model and save as .glb
        var model = sceneBuilder.ToGltf2();
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(glbPath))!);
        model.SaveGLB(glbPath);
    }

    #region Bone / skeleton helpers

    private sealed class BoneInfo
    {
        public string Name = string.Empty;
        public int Index;
        public System.Numerics.Matrix4x4 OffsetMatrix;          // inverse bind matrix from Assimp
        public Node? Node;                                      // corresponding Assimp node (if any)
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
                        OffsetMatrix = ToNumerics(bone.OffsetMatrix)
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
        Dictionary<string, BoneInfo> bones,
        out NodeBuilder? armatureRoot)
    {
        var nodeMap = new Dictionary<string, NodeBuilder>();

        // Local variable that the local function can close over
        NodeBuilder? armatureRootLocal = null;

        // Recursive local function to mirror Assimp's node tree into NodeBuilder hierarchy
        NodeBuilder BuildNode(Node assimpNode, NodeBuilder? parent)
        {
            var localTransform = ToNumerics(assimpNode.Transform);

            var nodeBuilder = new NodeBuilder(assimpNode.Name)
            {
                LocalTransform = localTransform
            };

            if (parent != null)
                parent.AddNode(nodeBuilder);

            nodeMap[assimpNode.Name] = nodeBuilder;

            // Heuristic: first bone node we encounter becomes our armature root
            if (bones.ContainsKey(assimpNode.Name) && armatureRootLocal == null)
                armatureRootLocal = nodeBuilder;

            foreach (var child in assimpNode.Children)
                BuildNode(child, nodeBuilder);

            return nodeBuilder;
        }

        // Build hierarchy starting at scene root
        BuildNode(scene.RootNode, null);

        // Fallback if no bone node matched: use scene root
        armatureRoot = armatureRootLocal ?? nodeMap[scene.RootNode.Name];

        // Wire Assimp node references into BoneInfo, so we know which Node is which bone
        void AttachNodesToBones(Node node)
        {
            if (bones.TryGetValue(node.Name, out var b))
                b.Node = node;

            foreach (var c in node.Children)
                AttachNodesToBones(c);
        }

        AttachNodesToBones(scene.RootNode);

        return nodeMap;
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
        Assimp.Mesh mesh,
        int index,
        Vector4[] boneIndices,
        Vector4[] boneWeights)
    {
        var pos = mesh.HasVertices ? mesh.Vertices[index] : new Assimp.Vector3D();
        var nrm = mesh.HasNormals ? mesh.Normals[index] : new Assimp.Vector3D(0, 1, 0);

        Assimp.Vector3D uv = default;
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

    private static System.Numerics.Matrix4x4 ToNumerics(Assimp.Matrix4x4 assimpMatrix)
    {
        // Assimp's Matrix4x4 has same layout as System.Numerics.Matrix4x4 (row-major),
        // but types are different, so we map field by field.
        return new System.Numerics.Matrix4x4(
            assimpMatrix.A1, assimpMatrix.B1, assimpMatrix.C1, assimpMatrix.D1,
            assimpMatrix.A2, assimpMatrix.B2, assimpMatrix.C2, assimpMatrix.D2,
            assimpMatrix.A3, assimpMatrix.B3, assimpMatrix.C3, assimpMatrix.D3,
            assimpMatrix.A4, assimpMatrix.B4, assimpMatrix.C4, assimpMatrix.D4
        );
    }

    #endregion
}
