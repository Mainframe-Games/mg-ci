using MainServer.Configs;
using MainServer.Services.Packets;
using MainServer.Utils;
using Newtonsoft.Json.Linq;
using ServerShared;
using SocketServer;

namespace MainServer.Services.Server;

internal sealed class BuildServerService(SocketServer.Server server, ServerConfig serverConfig)
    : ServerService(server)
{
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
        var data = payload.ToObject<BuildRunnerPacket>() ?? throw new NullReferenceException();

        var projectGuid = data.ProjectGuid;
        var buildTargetNames = data.BuildTargets;
        var branch = data.Branch;
        var gitUrl = data.GitUrl;

        if (ServerPipeline.ActiveProjects.Contains(projectGuid))
        {
            SendJson(new JObject { ["Error"] = $"Pipeline already exists: {projectGuid}" });
            return;
        }

        var workspace =
            WorkspaceUpdater.PrepareWorkspace(projectGuid, gitUrl, branch, serverConfig)
            ?? throw new NullReferenceException();
        var project = workspace.GetProjectToml();
        var pipeline = new ServerPipeline(projectGuid, workspace, buildTargetNames, serverConfig);
        pipeline.Run();

        SendJson(
            new JObject
            {
                // ["ServerVersion"] = ServerInfo.Version,
                // ["PipelineId"] = pipeline.ProjectId,
                ["ProjectName"] = project.GetValue<string>("settings", "project_name"),
                ["Targets"] = string.Join(", ", buildTargetNames),
                // ["UnityVersion"] = workspace.UnityVersion,
                // ["ChangesetId"] = changeSetId,
                // ["ChangesetGuid"] = guid,
                ["Branch"] = branch,
            }
        );
    }
}
