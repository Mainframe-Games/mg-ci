using System.Diagnostics;
using MainServer.Configs;
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

    private static readonly Queue<QueuePacket> _buildQueue = new();
    private static Task? _buildRunnerTask;

    private async void BuildQueued()
    {
        // prevent multiple build runners
        if (_buildRunnerTask is not null)
            return;

        while (_buildQueue.Count > 0)
        {
            var packet = _buildQueue.Dequeue();
            var projectGuid = packet.ProjectGuid;
            var targetName = packet.TargetName;
            var branch = packet.Branch;

            var workspace =
                WorkspaceUpdater.PrepareWorkspace(projectGuid, branch, serverConfig)
                ?? throw new NullReferenceException();

            Console.WriteLine($"Queuing build: {targetName}");
            await SendJson(new JObject { ["TargetName"] = targetName, ["Status"] = "Building" });

            _buildRunnerTask = workspace.Engine switch
            {
                GameEngine.Unity
                    => Task.Run(() => RunUnity(workspace.ProjectPath, targetName, workspace)),
                GameEngine.Godot => throw new NotImplementedException(),
                _ => throw new ArgumentException($"Engine not supported: {workspace.Engine}")
            };

            // await build to be done
            await _buildRunnerTask;
            _buildRunnerTask = null;
        }

        Console.WriteLine("Build queue empty");
    }

    private async void RunUnity(string projectPath, string targetName, Workspace workspace)
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
        var extension = target.GetValue<string>("extension") ?? throw new NullReferenceException();
        var product_name =
            target.GetValue<string>("product_name") ?? throw new NullReferenceException();
        var buildTargetName =
            target.GetValue<string>("target") ?? throw new NullReferenceException();
        var target_group =
            target.GetValue<string>("target_group") ?? throw new NullReferenceException();
        var sub_target =
            target.GetValue<string>("sub_target") ?? throw new NullReferenceException();
        var scenes = target.GetValue<List<string>>("scenes") ?? [];
        var extraScriptingDefines = target.GetValue<List<string>>("extra_scripting_defines") ?? [];
        var assetBundleManifestPath =
            target.GetValue<string>("asset_bundle_manifest_path") ?? string.Empty;
        var build_options = (int)target.GetValue<long>("build_options");

        var unityRunner = new UnityBuild(
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

        await SendJson(
            new JObject
            {
                ["TargetName"] = targetName,
                ["Status"] = "Complete",
                ["Time"] = sw.ElapsedMilliseconds
            }
        );

        // send back
        await FileUploader.UploadDirectory(new DirectoryInfo(unityRunner.BuildPath), this);
    }

    private class QueuePacket
    {
        public Guid ProjectGuid { get; set; }
        public string TargetName { get; set; }
        public string Branch { get; set; }
    }

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
        var targetName = payload["TargetName"]?.ToString() ?? throw new NullReferenceException();
        var projectGuid = payload["ProjectGuid"]?.ToString() ?? throw new NullReferenceException();
        var branch = payload["Branch"]?.ToString() ?? throw new NullReferenceException();

        var queuePacket = new QueuePacket
        {
            ProjectGuid = new Guid(projectGuid),
            TargetName = targetName,
            Branch = branch,
        };
        _buildQueue.Enqueue(queuePacket);
        await SendJson(
            new JObject { ["TargetName"] = queuePacket.TargetName, ["Status"] = "Queued", }
        );
        BuildQueued();
    }
}
