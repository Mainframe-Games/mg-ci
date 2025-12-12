using Assimp;
using SharpGLTF.Schema2;
using System.Numerics;
using MG_CLI;
using Spectre.Console;
using Node = SharpGLTF.Schema2.Node;
using Quaternion = System.Numerics.Quaternion;
using Scene = Assimp.Scene;

/// <summary>
/// Provides functionality to integrate FBX animations from a directory into a GLTF model
/// rigged with a Mixamo-compatible skeleton.
/// </summary>
public static class MixamoAnimations
{
    public static void AddAnimationsFromMixamoDir(
        in ModelRoot model,
        in string animsDir)
    {
        var nodeByName = model.LogicalNodes
            .Where(n => !string.IsNullOrWhiteSpace(n.Name))
            .GroupBy(n => n.Name!)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        using var ctx = new AssimpContext();

        var fbxFiles = Directory.GetFiles(animsDir, "*.fbx");
        Array.Sort(fbxFiles);

        foreach (var fbx in fbxFiles)
        {
            Log.Print($"[mixamo:anim] {Path.GetFileName(fbx)}");

            Scene scene;
            try
            {
                scene = ctx.ImportFile(fbx, PostProcessSteps.None);
            }
            catch (Exception ex)
            {
                Log.PrintError($"[mixamo:anim] Import failed: {ex.Message}");
                Log.Exception(ex);
                continue;
            }

            if (scene.AnimationCount == 0)
            {
                Log.Print($"[mixamo:anim] No animations found. {fbx}", Color.Yellow);
                continue;
            }

            foreach (var anim in scene.Animations)
            {
                var clipName = Path.GetFileNameWithoutExtension(fbx);
                clipName = clipName.ToSnakeCase();

                var tps = anim.TicksPerSecond;
                if (tps <= 0) tps = 30.0; // Assimp sometimes leaves this 0

                foreach (var ch in anim.NodeAnimationChannels)
                {
                    Node? target = null;
                    foreach (var cand in Candidates(ch.NodeName))
                    {
                        if (nodeByName.TryGetValue(cand, out target))
                            break;
                    }
                    
                    if (target == null) 
                        continue;

                    // Translation
                    if (ch.PositionKeyCount > 0)
                    {
                        var keys = new (float Key, Vector3 Value)[ch.PositionKeyCount];
                        for (int i = 0; i < ch.PositionKeyCount; i++)
                        {
                            var k = ch.PositionKeys[i];
                            keys[i] = ((float)(k.Time / tps),
                                       new Vector3(k.Value.X, k.Value.Y, k.Value.Z));
                        }

                        keys = DedupVec3(keys);
                        if (keys.Length > 0)
                            target.WithTranslationAnimation(clipName, keys);
                    }

                    // Rotation
                    if (ch.RotationKeyCount > 0)
                    {
                        var keys = new (float Key, Quaternion Value)[ch.RotationKeyCount];
                        for (int i = 0; i < ch.RotationKeyCount; i++)
                        {
                            var k = ch.RotationKeys[i];
                            keys[i] = ((float)(k.Time / tps),
                                       new Quaternion(k.Value.X, k.Value.Y, k.Value.Z, k.Value.W));
                        }

                        keys = DedupQuat(keys);
                        if (keys.Length > 0)
                            target.WithRotationAnimation(clipName, keys);
                    }

                    // Scale (optional; Mixamo often has none)
                    if (ch.ScalingKeyCount > 0)
                    {
                        var keys = new (float Key, Vector3 Value)[ch.ScalingKeyCount];
                        for (int i = 0; i < ch.ScalingKeyCount; i++)
                        {
                            var k = ch.ScalingKeys[i];
                            keys[i] = ((float)(k.Time / tps),
                                       new Vector3(k.Value.X, k.Value.Y, k.Value.Z));
                        }

                        keys = DedupVec3(keys);
                        if (keys.Length > 0)
                            target.WithScaleAnimation(clipName, keys);
                    }
                }
            }
        }
        
        return;

        static IEnumerable<string> Candidates(string name)
        {
            yield return name;

            // common Mixamo prefix
            const string p = "mixamorig:";
            if (name.StartsWith(p, StringComparison.OrdinalIgnoreCase))
                yield return name[p.Length..];
        }
    }
    
    private static (float Key, Vector3 Value)[] DedupVec3((float Key, Vector3 Value)[] keys)
    {
        if (keys.Length == 0) return keys;

        // sort by time
        Array.Sort(keys, (a, b) => a.Key.CompareTo(b.Key));

        var outList = new List<(float, Vector3)>(keys.Length);

        // collapse keys with same time (within epsilon)
        const float eps = 1e-6f;

        var curT = keys[0].Key;
        var curV = keys[0].Value;

        for (int i = 1; i < keys.Length; i++)
        {
            var t = keys[i].Key;
            var v = keys[i].Value;

            if (MathF.Abs(t - curT) <= eps)
            {
                // same timestamp -> keep the latest value
                curV = v;
            }
            else
            {
                outList.Add((curT, curV));
                curT = t;
                curV = v;
            }
        }

        outList.Add((curT, curV));
        return outList.ToArray();
    }

    private static (float Key, Quaternion Value)[] DedupQuat((float Key, Quaternion Value)[] keys)
    {
        if (keys.Length == 0) return keys;

        Array.Sort(keys, (a, b) => a.Key.CompareTo(b.Key));

        var outList = new List<(float, Quaternion)>(keys.Length);
        const float eps = 1e-6f;

        var curT = keys[0].Key;
        var curV = Quaternion.Normalize(keys[0].Value);

        for (int i = 1; i < keys.Length; i++)
        {
            var t = keys[i].Key;
            var v = Quaternion.Normalize(keys[i].Value);

            if (MathF.Abs(t - curT) <= eps)
            {
                // same timestamp -> keep the latest value
                curV = v;
            }
            else
            {
                outList.Add((curT, curV));
                curT = t;
                curV = v;
            }
        }

        outList.Add((curT, curV));
        return outList.ToArray();
    }

}
