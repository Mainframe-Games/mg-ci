using System.Diagnostics;
using Newtonsoft.Json.Linq;
using ServerShared;
using Tomlyn.Model;
using UnityBuilder;
using WebSocketSharp;

namespace OffloadServer;

internal class BuildRunnerService : ServiceBase
{
    private static readonly Queue<KeyValuePair<string, Guid>> _buildQueue = new();
    private static Task? _buildRunnerTask;

    protected override void OnOpen()
    {
        base.OnOpen();
        Console.WriteLine($"Client connected [{Context.RequestUri.AbsolutePath}]: {ID}");
    }

    protected override void OnMessage(MessageEventArgs e)
    {
        base.OnMessage(e);

        var payload = JObject.Parse(e.Data) ?? throw new NullReferenceException();
        var targetName = payload["TargetName"]?.ToString() ?? throw new NullReferenceException();
        var projectGuid = payload["ProjectGuid"]?.ToString() ?? throw new NullReferenceException();

        var item = new KeyValuePair<string, Guid>(targetName, new Guid(projectGuid));
        _buildQueue.Enqueue(item);
        Send(new JObject { ["TargetName"] = item.Key, ["Status"] = "Queued", }.ToString());
        BuildQueued();
    }

    private async void BuildQueued()
    {
        // prevent multiple build runners
        if (_buildRunnerTask is not null)
            return;

        while (_buildQueue.Count > 0)
        {
            var (targetName, projectGuid) = _buildQueue.Dequeue();
            
            var workspace =
                WorkspaceUpdater.PrepareWorkspace(projectGuid) ?? throw new NullReferenceException();

            Console.WriteLine($"Queuing build: {targetName}");
            Send(new JObject { ["TargetName"] = targetName, ["Status"] = "Building" }.ToString());

            _buildRunnerTask = workspace.Engine switch
            {
                GameEngine.Unity => Task.Run(() => RunUnity(workspace.ProjectPath, targetName, workspace)),
                GameEngine.Godot => throw new NotImplementedException(),
                _ => throw new ArgumentException($"Engine not supported: {workspace.Engine}")
            };

            // await build to be done
            await _buildRunnerTask;
            _buildRunnerTask = null;
        }

        Console.WriteLine("Build queue empty");
    }

    private void RunUnity(string projectPath, string targetName, Workspace workspace)
    {
        var project = workspace.GetProjectToml();
        var isBuildTargets = project.TryGetValue("build_targets", out var buildTargets);
        
        if (!isBuildTargets)
            throw new Exception("build_targets not found in toml");

        if (buildTargets is not TomlTableArray array)
            throw new Exception("buildTargets is not an array");

        var target = array.FirstOrDefault(x => x["name"]?.ToString() == targetName)?? throw new NullReferenceException();
        
        // run build
        var extension = target["extension"]?.ToString() ?? throw new NullReferenceException();
        var product_name = target["product_name"]?.ToString() ?? throw new NullReferenceException();
        var buildTargetName = target["build_target"]?.ToString() ?? throw new NullReferenceException();
        var target_group = target["target_group"]?.ToString() ?? throw new NullReferenceException();
        var sub_target = target["sub_target"]?.ToString() ?? throw new NullReferenceException();
        var scenes = (List<string>)(target["scenes"] ?? throw new NullReferenceException());
        var extraScriptingDefines = (List<string>)(target["extra_scripting_defines"] ?? throw new NullReferenceException());
        var assetBundleManifestPath = target["asset_bundle_manifest_path"]?.ToString() ?? throw new NullReferenceException();
        var build_options = (int)(target["build_options"] ?? throw new NullReferenceException());
        
        var unityRunner = new UnityBuild2(
            projectPath,
            targetName,
            extension,
            product_name,
            buildTargetName, 
            target_group, 
            sub_target, 
            scenes.ToArray(),
            extraScriptingDefines.ToArray(),
            assetBundleManifestPath,
            build_options
        );
        
        var sw = Stopwatch.StartNew();
        unityRunner.Run();
        
        Send(
            new JObject
            {
                ["TargetName"] = targetName,
                ["Status"] = "Complete",
                ["Time"] = sw.ElapsedMilliseconds
            }.ToString()
        );
        
        // send back
        UploadDirectory(unityRunner.BuildPath, targetName);
    }

    private async void UploadDirectory(string buildPath, string targetName)
    {
        var rootDir = new DirectoryInfo(buildPath);
        var allFiles = rootDir.GetFiles("*", SearchOption.AllDirectories);
        var totalBytes = allFiles.Sum(x => (int)x.Length);
        foreach (var file in allFiles)
            await SendFile(file, rootDir.FullName, targetName, totalBytes);
    }

    private async Task SendFile(
        FileSystemInfo fileInfo,
        string rootPath,
        string targetName,
        int totalLength
    )
    {
        const int fragmentSize = 1024 * 10; // 10 KB

        var data = await File.ReadAllBytesAsync(fileInfo.FullName);

        // Calculate the number of fragments
        var totalFragments = (int)Math.Ceiling((double)data.Length / fragmentSize);

        var fileLocalName = fileInfo
            .FullName.Replace(rootPath, string.Empty)
            .Replace('\\', '/')
            .Trim('/');

        Console.WriteLine($"Sending file [{data.Length} bytes]: {fileInfo.FullName}");

        // Send each fragment
        for (int i = 0; i < totalFragments; i++)
        {
            // Calculate the start and end index for the current fragment
            var startIndex = i * fragmentSize;
            var endIndex = Math.Min((i + 1) * fragmentSize, data.Length);

            // Extract the current fragment from the original data
            var fragment = new byte[endIndex - startIndex];
            Array.Copy(data, startIndex, fragment, 0, fragment.Length);

            // Write the target name, total length, and fragment to a memory stream
            using var ms = new MemoryStream();
            await using var writer = new BinaryWriter(ms);
            writer.Write(targetName); // string
            writer.Write(totalLength); // int32
            writer.Write(fileLocalName); // string
            writer.Write(fragment.Length); // int32
            writer.Write(fragment); // byte[]

            // Send the fragment
            Send(ms.ToArray());
            await Task.Delay(10);
        }
    }
}
