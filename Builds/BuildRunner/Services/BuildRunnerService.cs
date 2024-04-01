using BuildRunner.Utils;
using Newtonsoft.Json.Linq;
using Server.Services;
using UnityBuilder;
using WebSocketSharp;

namespace BuildRunner;

public class BuildRunnerService : ServiceBase
{
    private static readonly Dictionary<string, bool> _platform =
        new()
        {
            ["windows"] = Arg.IsFlag("-windows"),
            ["macos"] = Arg.IsFlag("-macos"),
            ["linux"] = Arg.IsFlag("-linux"),
        };

    private static readonly Queue<JObject> _buildQueue = new();
    private static Task? _buildRunnerTask;

    protected override void OnOpen()
    {
        base.OnOpen();
        Console.WriteLine($"Client connected [{Context.RequestUri.AbsolutePath}]: {ID}");

        var keys = _platform.Where(x => x.Value).Select(x => x.Key).ToArray();
        var platforms = string.Join(" ", keys).Trim();
        if (platforms.Length > 0)
            Send(platforms);
    }

    protected override void OnMessage(MessageEventArgs e)
    {
        base.OnMessage(e);

        var payload = JObject.Parse(e.Data) ?? throw new NullReferenceException();
        _buildQueue.Enqueue(payload);
        BuildQueued();
    }

    private async void BuildQueued()
    {
        // prevent multiple build runners
        if (_buildRunnerTask is not null)
            return;

        while (_buildQueue.Count > 0)
        {
            var payload = _buildQueue.Dequeue();
            var projectId =
                payload.SelectToken("Project.Guid", true)?.ToString()
                ?? throw new NullReferenceException();
            var workspace =
                WorkspaceUpdater.PrepareWorkspace(projectId) ?? throw new NullReferenceException();

            var buildTarget =
                payload["BuildTarget"]?.ToString() ?? throw new NullReferenceException();

            Console.WriteLine($"Queuing build: {buildTarget}");
            Send(new JObject { ["Build"] = buildTarget, ["Status"] = "Queued", }.ToString());

            _buildRunnerTask = workspace.Engine switch
            {
                "Unity" => Task.Run(() => RunUnity(workspace.ProjectPath, payload)),
                "Godot" => throw new NotImplementedException(),
                _ => throw new ArgumentException()
            };

            // await build to be done
            await _buildRunnerTask;
            _buildRunnerTask = null;
        }

        Console.WriteLine("Build queue empty");
    }

    private static void RunUnity(string projectPath, JObject payload)
    {
        var targetName = payload["TargetName"]?.ToString() ?? throw new NullReferenceException();
        var buildTarget = payload["BuildTarget"]?.ToString() ?? throw new NullReferenceException();
        var target =
            payload
                .SelectToken("Project.BuildTargets", true)
                ?.FirstOrDefault(x => x["Name"]?.ToString() == targetName)
            ?? throw new NullReferenceException();

        // run build
        var unityRunner = new UnityBuild2(projectPath, target, buildTarget);
        unityRunner.Run();

        // todo: zip build content and send back

        // SendAsync();
    }
}
