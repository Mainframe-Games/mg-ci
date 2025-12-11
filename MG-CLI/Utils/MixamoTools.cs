using Assimp;
using SharpGLTF.Schema2;
using Animation = Assimp.Animation;
using Node = Assimp.Node;
using Scene = Assimp.Scene;

namespace MG_CLI;

public static class MixamoTools
{
    // --- CONFIG: required bones for a “reasonable” Mixamo humanoid rig ---
    private static readonly string[] RequiredBoneNames =
    {
        "Hips", "Spine", "Spine1", "Spine2",
        "Neck", "Head",
        "LeftShoulder", "LeftArm", "LeftForeArm", "LeftHand",
        "RightShoulder", "RightArm", "RightForeArm", "RightHand",
        "LeftUpLeg", "LeftLeg", "LeftFoot",
        "RightUpLeg", "RightLeg", "RightFoot"
    };

    /// <summary>
    /// Validates that the FBX looks like a Mixamo rig.
    /// Returns true if “looks ok”, false if missing bones / other issues.
    /// Also prints human-readable info to the console.
    /// </summary>
    public static bool ValidateMixamoRig(string fbxPath)
    {
        if (!File.Exists(fbxPath))
        {
            Log.PrintError($"[mixamo:validate] FBX not found: {fbxPath}");
            return false;
        }

        var context = new AssimpContext();

        const PostProcessSteps flags =
            PostProcessSteps.Triangulate |
            PostProcessSteps.JoinIdenticalVertices |
            PostProcessSteps.LimitBoneWeights |
            PostProcessSteps.ValidateDataStructure;

        Scene scene;
        try
        {
            scene = context.ImportFile(fbxPath, flags);
        }
        catch (Exception ex)
        {
            Log.PrintError($"[mixamo:validate] Failed to import FBX: {ex.Message}");
            return false;
        }

        if (scene == null || !scene.HasMeshes)
        {
            Log.PrintError("[mixamo:validate] Scene has no meshes.");
            return false;
        }

        // Collect all bone names used by skinned meshes
        var meshBones = new HashSet<string>(
            scene.Meshes
                 .SelectMany(m => m.Bones)
                 .Select(b => b.Name)
                 .Distinct());

        if (meshBones.Count == 0)
        {
            Log.PrintError("[mixamo:validate] No bones found in meshes (not skinned?).");
            return false;
        }

        Log.Print("[mixamo:validate] Found bone count: " + meshBones.Count);

        // Try to guess the root bone (Hips or mixamorig:Hips etc.)
        string? rootBone = meshBones.FirstOrDefault(n =>
            string.Equals(n, "Hips", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(n, "mixamorig:Hips", StringComparison.OrdinalIgnoreCase));

        if (rootBone == null)
        {
            Log.PrintError("[mixamo:validate] Could not find root bone 'Hips' or 'mixamorig:Hips'.");
        }
        else
        {
            Log.Print("[mixamo:validate] Root bone candidate: " + rootBone);
        }

        // Check missing core bones
        var missingRequired = RequiredBoneNames
            .Where(req =>
                !meshBones.Contains(req) &&
                !meshBones.Contains("mixamorig:" + req))
            .ToList();

        if (missingRequired.Any())
        {
            Log.PrintError("[mixamo:validate] Missing expected Mixamo bones:");
            foreach (var b in missingRequired)
                Log.PrintError("  - " + b);
        }
        else
        {
            Log.Print("[mixamo:validate] All core Mixamo bones present ✅");
        }

        // Check that all mesh bones exist in the node hierarchy
        var nodeNames = new HashSet<string>();
        CollectNodeNames(scene.RootNode, nodeNames);

        var orphanBones = meshBones
            .Where(b => !nodeNames.Contains(b))
            .ToList();

        if (orphanBones.Any())
        {
            Log.PrintError("[mixamo:validate] Some bones are not present as nodes in the hierarchy:");
            foreach (var b in orphanBones)
                Log.PrintError("  - " + b);
        }

        var looksLikeMixamo = rootBone != null &&
                              !missingRequired.Any() &&
                              !orphanBones.Any();

        if (looksLikeMixamo)
        {
            Log.Print("[mixamo:validate] ✅ Rig looks Mixamo-compatible.");
        }
        else
        {
            Log.PrintError("[mixamo:validate] ⚠ Rig does NOT fully match expected Mixamo structure.");
        }

        return looksLikeMixamo;
    }

    private static void CollectNodeNames(Node node, HashSet<string> names)
    {
        names.Add(node.Name);
        foreach (var child in node.Children)
            CollectNodeNames(child, names);
    }

    /// <summary>
    /// Combines a base Mixamo-rigged FBX + multiple Mixamo animation FBXs
    /// into a single glTF2 file, then repacks to GLB for Godot.
    /// </summary>
    public static void BuildCombinedGodotGlb(
        string baseFbxPath,
        string animsDir,
        string outputGlbPath)
    {
        if (!File.Exists(baseFbxPath))
            throw new FileNotFoundException("Base FBX not found", baseFbxPath);
        if (!Directory.Exists(animsDir))
            throw new DirectoryNotFoundException(animsDir);

        var context = new AssimpContext();

        const PostProcessSteps baseFlags =
            PostProcessSteps.Triangulate |
            PostProcessSteps.JoinIdenticalVertices |
            PostProcessSteps.ImproveCacheLocality |
            PostProcessSteps.LimitBoneWeights |
            PostProcessSteps.ValidateDataStructure;

        // 1) Import base character (mesh + skeleton)
        var baseScene = context.ImportFile(baseFbxPath, baseFlags);
        if (baseScene == null || !baseScene.HasMeshes)
            throw new InvalidOperationException("Base FBX import failed or has no meshes.");

        Log.Print($"[mixamo:build] Base meshes: {baseScene.MeshCount}, bones: " +
                          baseScene.Meshes.Sum(m => m.BoneCount));
        Log.Print($"[mixamo:build] Base animations: {baseScene.AnimationCount}");

        // 2) Import each Mixamo animation FBX and copy animations into baseScene
        var fbxFiles = Directory.GetFiles(animsDir, "*.fbx");
        Array.Sort(fbxFiles);

        foreach (var animFile in fbxFiles)
        {
            Log.Print($"\n[mixamo:build] Importing animation FBX: {Path.GetFileName(animFile)}");

            var animScene = context.ImportFile(animFile, PostProcessSteps.None);
            if (animScene == null || !animScene.HasAnimations)
            {
                Log.Print("  No animations found, skipping.");
                continue;
            }

            Log.Print($"  Found {animScene.AnimationCount} animation(s)");

            foreach (var srcAnim in animScene.Animations)
            {
                var clipName = Path.GetFileNameWithoutExtension(animFile);
                Log.Print($"  Adding clip: {clipName}");

                var cloned = CloneAnimation(srcAnim, clipName);
                baseScene.Animations.Add(cloned);
            }
        }

        Log.Print($"\n[mixamo:build] Total animations in combined scene: {baseScene.AnimationCount}");

        // 3) Export to glTF2 (.gltf + .bin)
        var tempGltf = Path.ChangeExtension(outputGlbPath, ".gltf");
        Directory.CreateDirectory(Path.GetDirectoryName(tempGltf)!);

        Log.Print($"[mixamo:build] Exporting intermediate glTF2: {tempGltf}");
        context.ExportFile(baseScene, tempGltf, "gltf2");

        // 4) Repack to .glb using SharpGLTF
        Log.Print($"[mixamo:build] Repacking to .glb: {outputGlbPath}");
        var model = ModelRoot.Load(tempGltf);
        Directory.CreateDirectory(Path.GetDirectoryName(outputGlbPath)!);
        model.Save(outputGlbPath);

        Log.Print("[mixamo:build] ✅ Done. GLB ready for Godot.");
    }

    private static Animation CloneAnimation(Animation src, string newName)
    {
        var a = new Animation
        {
            Name = newName,
            DurationInTicks = src.DurationInTicks,
            TicksPerSecond = src.TicksPerSecond
        };

        foreach (var channel in src.NodeAnimationChannels)
        {
            var c = new NodeAnimationChannel
            {
                NodeName = channel.NodeName,
                PreState = channel.PreState,
                PostState = channel.PostState
            };

            foreach (var key in channel.PositionKeys)
                c.PositionKeys.Add(key);

            foreach (var key in channel.RotationKeys)
                c.RotationKeys.Add(key);

            foreach (var key in channel.ScalingKeys)
                c.ScalingKeys.Add(key);

            a.NodeAnimationChannels.Add(c);
        }

        return a;
    }
}
