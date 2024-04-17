using System.Diagnostics;
using MainServer.Configs;
using MainServer.Services.Packets;
using MainServer.Utils;
using MainServer.Workspaces;
using Newtonsoft.Json.Linq;
using ServerShared;
using SocketServer;
using SocketServer.Utils;
using Tomlyn.Model;
using UnityBuilder;

namespace MainServer.Services.Server;

internal sealed class BuildRunnerServerService(
    SocketServer.Server server,
    ServerConfig serverConfig
) : ServerService(server)
{
    public override string Name => "build-runner";
    private static readonly Queue<BuildQueueItem> _buildQueue = new();
    private Task? _buildTask;

    public override void OnStringMessage(string message)
    {
        throw new NotImplementedException();
    }

    public override void OnDataMessage(byte[] data)
    {
        throw new NotImplementedException();
    }

    public override async void OnJsonMessage(JObject payload)
    {
        var packet = payload.ToObject<BuildRunnerPacket>() ?? throw new NullReferenceException();

        foreach (var buildTarget in packet.BuildTargets)
        {
            _buildQueue.Enqueue(
                new BuildQueueItem
                {
                    ProjectGuid = packet.ProjectGuid,
                    GitUrl = packet.GitUrl,
                    Branch = packet.Branch,
                    BuildTarget = buildTarget
                }
            );

            await SendJson(new JObject { ["TargetName"] = buildTarget, ["Status"] = "Queued", });
        }

        if (_buildTask is not null)
            return;

        _buildTask = BuildQueued();
        await _buildTask;
        _buildTask = null;
    }

    private async Task BuildQueued()
    {
        while (_buildQueue.Count > 0)
        {
            var packet = _buildQueue.Dequeue();
            var projectGuid = packet.ProjectGuid;
            var targetName = packet.BuildTarget;
            var gitUrl = packet.GitUrl;
            var branch = packet.Branch;

            var workspace =
                WorkspaceUpdater.PrepareWorkspace(projectGuid, gitUrl, branch, serverConfig)
                ?? throw new NullReferenceException();

            Console.WriteLine($"Queuing build: {targetName}");
            await SendJson(new JObject { ["TargetName"] = targetName, ["Status"] = "Building" });

            var sw = Stopwatch.StartNew();

            var outputDir = workspace.Engine switch
            {
                GameEngine.Unity => RunUnity(projectGuid, workspace.ProjectPath, targetName, workspace),
                GameEngine.Godot => throw new NotImplementedException(),
                _ => throw new ArgumentException($"Engine not supported: {workspace.Engine}")
            };

            await SendJson(
                new JObject
                {
                    ["TargetName"] = targetName,
                    ["Status"] = "Complete",
                    ["Time"] = sw.ElapsedMilliseconds,
                    ["OutputDirectoryName"] = outputDir.Name
                }
            );
        }

        Console.WriteLine("Build queue empty");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="projectGuid"></param>
    /// <param name="projectPath"></param>
    /// <param name="targetName"></param>
    /// <param name="workspace"></param>
    /// <returns>Output Directory</returns>
    /// <exception cref="Exception"></exception>
    /// <exception cref="NullReferenceException"></exception>
    private DirectoryInfo RunUnity(
        Guid projectGuid,
        string projectPath,
        string targetName,
        Workspace workspace
    )
    {
        var project = workspace.GetProjectToml();
        var isBuildTargets = project.TryGetValue("build_targets", out var buildTargets);

        if (!isBuildTargets)
            throw new Exception("build_targets not found in toml");

        if (buildTargets is not TomlTableArray array)
            throw new Exception("buildTargets is not an array");

        var target =
            array.FirstOrDefault(x => x["name"]?.ToString() == targetName)
            ?? throw new NullReferenceException();

        // run build
        var product_name =
            target.GetValue<string>("product_name") ?? throw new NullReferenceException();
        var buildTarget = target.GetValue<string>("target") ?? throw new NullReferenceException();
        var target_group =
            target.GetValue<string>("target_group") ?? throw new NullReferenceException();
        var sub_target =
            target.GetValue<string>("sub_target") ?? throw new NullReferenceException();
        var scenes = target.GetList<string>("scenes") ?? [];
        var extraScriptingDefines = target.GetList<string>("extra_scripting_defines") ?? [];
        var assetBundleManifestPath =
            target.GetValue<string>("asset_bundle_manifest_path") ?? string.Empty;
        var build_options = (int)target.GetValue<long>("build_options");

        var unityRunner = new UnityBuild(
            projectPath,
            targetName,
            product_name,
            buildTarget,
            target_group,
            sub_target,
            scenes.ToArray(),
            extraScriptingDefines.ToArray(),
            assetBundleManifestPath,
            build_options
        );
        unityRunner.Run();

        var path = Path.Combine(projectPath, "Builds", $"{product_name}_{buildTarget}");
        var rootDir = new DirectoryInfo(path);

        if (server.IsSameMachine())
        {
            // runner server is on same machine as the main server,
            // so we just copy the directory
            FileCopier.Copy(projectGuid, rootDir);
        }
        else
        {
            // runner server is on a different machine,
            // so we need to upload it to the main server
            FileUploader.UploadDirectory(projectGuid, rootDir, this);
        }

        return rootDir;
    }

    private class BuildQueueItem
    {
        public Guid ProjectGuid { get; set; }
        public string BuildTarget { get; set; } = string.Empty;
        public string GitUrl { get; set; } = string.Empty;
        public string Branch { get; set; } = string.Empty;
    }
}
