using MainServer.Configs;
using MainServer.Utils;
using Newtonsoft.Json.Linq;
using ServerShared;
using SocketServer;

namespace MainServer.Services.Server;

internal sealed class BuildServerService(SocketServer.Server server, ServerConfig serverConfig)
    : ServerService(server)
{
    private async void StartBuild(Guid projectGuid, string[] buildTargetNames, string branch)
    {
        if (ServerPipeline.ActiveProjects.Contains(projectGuid))
        {
            await SendJson(new JObject { ["Error"] = $"Pipeline already exists: {projectGuid}" });
            return;
        }

        var workspace =
            WorkspaceUpdater.PrepareWorkspace(projectGuid, branch, serverConfig)
            ?? throw new NullReferenceException();
        var project = workspace.GetProjectToml();
        var pipeline = new ServerPipeline(projectGuid, workspace, buildTargetNames);
        pipeline.Run();

        await SendJson(
            new JObject
            {
                // ["ServerVersion"] = ServerInfo.Version,
                // ["PipelineId"] = pipeline.ProjectId,
                ["ProjectName"] = project.GetValue<string>("settings", "product_name"),
                ["Targets"] = string.Join(", ", buildTargetNames),
                // ["UnityVersion"] = workspace.UnityVersion,
                // ["ChangesetId"] = changeSetId,
                // ["ChangesetGuid"] = guid,
                ["Branch"] = branch,
            }
        );
    }

    public override string Name => "build";

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
        var projectId = payload["ProjectGuid"]?.ToString() ?? throw new NullReferenceException();
        var buildTargetNames =
            payload["BuildTargets"]?.ToObject<string[]>() ?? Array.Empty<string>();
        var branch = payload["Branch"]?.ToString() ?? throw new NullReferenceException();
        var projectGuid = new Guid(projectId);
        StartBuild(projectGuid, buildTargetNames, branch);
    }
}
