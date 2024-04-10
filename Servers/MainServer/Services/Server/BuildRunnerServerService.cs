using System.Diagnostics;
using MainServer.Configs;
using MainServer.Services.Packets;
using MainServer.Utils;
using MainServer.Workspaces;
using Newtonsoft.Json.Linq;
using ServerShared;
using SocketServer;
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

    public override void OnJsonMessage(JObject payload)
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

            SendJson(new JObject { ["TargetName"] = buildTarget, ["Status"] = "Queued", });
        }

        if (_buildTask is null || _buildTask.IsCompleted)
            _buildTask = Task.Run(BuildQueued);
    }

    private void BuildQueued()
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
            SendJson(new JObject { ["TargetName"] = targetName, ["Status"] = "Building" });

            switch (workspace.Engine)
            {
                case GameEngine.Unity:
                    RunUnity(projectGuid, workspace.ProjectPath, targetName, workspace);
                    break;
                case GameEngine.Godot:
                    throw new NotImplementedException();
                default:
                    throw new ArgumentException($"Engine not supported: {workspace.Engine}");
            }
        }

        Console.WriteLine("Build queue empty");
    }

    private void RunUnity(
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

        var sw = Stopwatch.StartNew();
        unityRunner.Run();

        SendJson(
            new JObject
            {
                ["TargetName"] = targetName,
                ["Status"] = "Complete",
                ["Time"] = sw.ElapsedMilliseconds
            }
        );

        // send back
        var path = Path.Combine(projectPath, "Builds", $"{product_name}_{buildTarget}");
        FileUploader.UploadDirectory(projectGuid, new DirectoryInfo(path), this);
    }

    private class BuildQueueItem
    {
        public Guid ProjectGuid { get; set; }
        public string BuildTarget { get; set; } = string.Empty;
        public string GitUrl { get; set; } = string.Empty;
        public string Branch { get; set; } = string.Empty;
    }
}
