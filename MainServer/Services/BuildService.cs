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
        var guid = new Guid(projectId);
        StartBuild(guid, buildTargetNames);
    }

    private void StartBuild(Guid projectGuid, IEnumerable<string> buildTargetNames)
    {
        if (ServerPipeline.ActiveProjects.Contains(projectGuid))
        {
            Send(new JObject
            {
                ["Error"] = $"Pipeline already exists: {projectGuid}"
            }.ToString());
            return;
        }

        var workspace = WorkspaceUpdater.PrepareWorkspace(projectGuid) ?? throw new NullReferenceException();
        var pipeline = new ServerPipeline(projectGuid, workspace, buildTargetNames);
        pipeline.Run();
        
        Send(new JObject
        {
            // ServerVersion = App.ServerVersion,
            // PipelineId = pipeline.ProjectId,
            // Workspace = workspace.Name,
            // WorkspaceMeta = workspace.Meta,
            // Targets = string.Join(", ", workspace.GetBuildTargets().Select(x => x.Name)),
            // UnityVersion = workspace.UnityVersion,
            // ChangesetId = changeSetId,
            // ChangesetGuid = guid,
            // Branch = project.Settings.Branch,
            // ChangesetCount = pipeline.ChangeLog.Length,

        }.ToString());
    }

    // private ServerResponse StartBuildPlastic(Project project)
    // {
    //     var workspaceName = new WorkspaceMapping().GetRemapping(project.Settings.ProjectName);
    //     var workspace = PlasticWorkspace.GetWorkspaceFromName(workspaceName);
    //
    //     if (workspace is null)
    //         return new ServerResponse(
    //             HttpStatusCode.BadRequest,
    //             $"Given namespace is not valid: {project.Settings.ProjectName}"
    //         );
    //
    //     Logger.Log($"Chosen workspace: {workspace}");
    //     var res = StartBuildPipeline(workspace, project);
    //     return res;
    // }

    // private ServerResponse StartBuildPipeline(Workspace workspace, Project project)
    // {
    //     var args = new Args(""); // TODO: remove args from pipeline, everything should be done in C# classes
    //
    //     var pipeline = App.CreateBuildPipeline(workspace, args, project);
    //     pipeline.Report.OnReportUpdated += OnReportUpdated;
    //
    //     App.RunBuildPipe(pipeline).FireAndForget();
    //     workspace.GetCurrent(out var changeSetId, out var guid);
    //
    //     var data = new BuildPipelineResponse
    //     {
    //         ServerVersion = App.ServerVersion,
    //         PipelineId = pipeline.ProjectId,
    //         Workspace = workspace.Name,
    //         WorkspaceMeta = workspace.Meta,
    //         Targets = string.Join(", ", workspace.GetBuildTargets().Select(x => x.Name)),
    //         UnityVersion = workspace.UnityVersion,
    //         ChangesetId = changeSetId,
    //         ChangesetGuid = guid,
    //         Branch = project.Settings.Branch,
    //         ChangesetCount = pipeline.ChangeLog.Length,
    //     };
    //     return new ServerResponse(HttpStatusCode.OK, data);
    // }
    //
    // private void OnReportUpdated(PipelineReport report)
    // {
    //     var body = new Dictionary<string, object>
    //     {
    //         // ["CommandId"] = Content.CommandId,
    //         ["Report"] = report,
    //     };
    //
    //     Sessions.Broadcast(Json.Serialise(body));
    // }
}
