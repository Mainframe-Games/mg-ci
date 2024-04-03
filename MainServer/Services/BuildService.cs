using Newtonsoft.Json.Linq;
using ServerShared;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace MainServer.Services;

public class BuildService : WebSocketBehavior
{
    protected override void OnMessage(MessageEventArgs e)
    {
        base.OnMessage(e);
        
        var response = JObject.Parse(e.Data);
        var projectId = response["Guid"]?.ToString() ?? throw new NullReferenceException();
        var buildTargetNames = response["BuildTargets"]?.ToObject<string[]>() ?? Array.Empty<string>();
        var branch = response["Branch"]?.ToString() ?? throw new NullReferenceException();
        var projectGuid = new Guid(projectId);
        StartBuild(projectGuid, buildTargetNames, branch);
    }

    private void StartBuild(Guid projectGuid, string[] buildTargetNames, string branch)
    {
        if (ServerPipeline.ActiveProjects.Contains(projectGuid))
        {
            Send(new JObject
            {
                ["Error"] = $"Pipeline already exists: {projectGuid}"
            }.ToString());
            return;
        }

        var workspace = WorkspaceUpdater.PrepareWorkspace(projectGuid, branch) ?? throw new NullReferenceException();
        var project = workspace.GetProjectToml();
        var pipeline = new ServerPipeline(projectGuid, workspace, buildTargetNames);
        pipeline.Run();
        
        Send(new JObject
        {
            // ["ServerVersion"] = ServerInfo.Version,
            // ["PipelineId"] = pipeline.ProjectId,
            ["ProjectName"] = project.GetValue<string>("settings", "product_name"),
            ["Targets"] = string.Join(", ", buildTargetNames),
            // ["UnityVersion"] = workspace.UnityVersion,
            // ["ChangesetId"] = changeSetId,
            // ["ChangesetGuid"] = guid,
            ["Branch"] = branch,

        }.ToString());
    }
}
