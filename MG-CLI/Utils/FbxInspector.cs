using System.Text;
using Assimp;
using Spectre.Console;

namespace MG_CLI;

public static class FbxInspector
{
    public static void InspectFbx(string fbxPath)
    {
        var str = new StringBuilder();
        
        if (!File.Exists(fbxPath))
        {
            Log.PrintError($"[inspect] FBX not found: {fbxPath}");
            return;
        }

        Log.Print($"[inspect] FBX: {Path.GetFullPath(fbxPath)}");

        using var context = new AssimpContext();

        LogStream.IsVerboseLoggingEnabled = true;
        var log = new LogStream((msg, _) => Log.Print("[ASSIMP] " + msg));
        log.Attach();

        const PostProcessSteps flags =
            PostProcessSteps.Triangulate |
            PostProcessSteps.JoinIdenticalVertices |
            PostProcessSteps.ImproveCacheLocality |
            PostProcessSteps.LimitBoneWeights;

        Scene? scene;
        try
        {
            scene = context.ImportFile(fbxPath, flags);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("[inspect] ImportFile threw:");
            Console.Error.WriteLine("  " + ex.GetType().Name + ": " + ex.Message);
            return;
        }

        if (scene == null)
        {
            Console.Error.WriteLine("[inspect] Import failed: scene is null.");
            return;
        }

        str.AppendLine("=== SCENE ===");
        str.AppendLine($"Meshes:      {scene.MeshCount}");
        str.AppendLine($"Animations:  {scene.AnimationCount}");
        str.AppendLine($"Materials:   {scene.MaterialCount}");

        str.AppendLine();
        str.AppendLine("=== MESHES ===");
        for (int mi = 0; mi < scene.MeshCount; mi++)
        {
            var mesh = scene.Meshes[mi];
            str.AppendLine($"Mesh[{mi}]: {mesh.Name}");
            str.AppendLine($"  Vertices:   {mesh.VertexCount}");
            str.AppendLine($"  Faces:      {mesh.FaceCount}");
            str.AppendLine($"  HasNormals: {mesh.HasNormals}");
            str.AppendLine($"  UVChannels: {mesh.TextureCoordinateChannelCount}");
            str.AppendLine($"  Bones:      {mesh.BoneCount}");

            for (int bi = 0; bi < mesh.BoneCount; bi++)
            {
                var bone = mesh.Bones[bi];
                str.AppendLine($"    Bone[{bi}]: {bone.Name}");
                str.AppendLine($"      Weights: {bone.VertexWeightCount}");
                // OffsetMatrix is usually the inverse bind pose:
                var m = bone.OffsetMatrix;
                str.AppendLine(
                    $"      OffsetMatrix: [{m.A1:0.###} {m.A2:0.###} {m.A3:0.###} {m.A4:0.###} | " +
                    $"{m.B1:0.###} {m.B2:0.###} {m.B3:0.###} {m.B4:0.###} | " +
                    $"{m.C1:0.###} {m.C2:0.###} {m.C3:0.###} {m.C4:0.###} | " +
                    $"{m.D1:0.###} {m.D2:0.###} {m.D3:0.###} {m.D4:0.###}]");
            }
        }

        str.AppendLine();
        str.AppendLine("=== NODE HIERARCHY (first 3 levels) ===");
        PrintNode(scene.RootNode, 0, maxDepth: 3);

        str.AppendLine();
        str.AppendLine("=== ANIMATIONS ===");
        for (int ai = 0; ai < scene.AnimationCount; ai++)
        {
            var anim = scene.Animations[ai];
            str.AppendLine($"Anim[{ai}]: {anim.Name}");
            str.AppendLine($"  Duration: {anim.DurationInTicks} ticks @ {anim.TicksPerSecond} tps");
            str.AppendLine($"  Channels: {anim.NodeAnimationChannelCount}");

            foreach (var ch in anim.NodeAnimationChannels)
            {
                str.AppendLine($"    Channel Node: {ch.NodeName}");
                str.AppendLine($"      Pos keys: {ch.PositionKeyCount}");
                str.AppendLine($"      Rot keys: {ch.RotationKeyCount}");
                str.AppendLine($"      Scl keys: {ch.ScalingKeyCount}");
            }
        }

        str.AppendLine();
        Log.Print("[inspect] Done.", Color.Green);

        var dir = Path.GetDirectoryName(fbxPath);
        var fileName = Path.GetFileName(fbxPath);
        var outputPath = Path.Combine(dir!, $"{fileName}.inspect.txt");
        File.WriteAllText(outputPath, str.ToString());
        Log.Print($"[inspect] Logged output to: {outputPath}", Color.Green);
    }

    private static void PrintNode(Node node, int depth, int maxDepth)
    {
        var indent = new string(' ', depth * 2);
        Log.Print($"{indent}- {node.Name} (Children: {node.ChildCount}, Meshes: {string.Join(",", node.MeshIndices)})");

        if (depth >= maxDepth) return;

        foreach (var child in node.Children)
            PrintNode(child, depth + 1, maxDepth);
    }
}
