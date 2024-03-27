using System.Net;
using Deployment.RemoteBuild;
using SharedLib;
using SharedLib.BuildToDiscord;
using SharedLib.Server;

namespace Server.Endpoints;

/// <summary>
/// Builds the entire workspace from buildconfig.json (used for master build server)
/// </summary>
public class BuildWorkspace : Endpoint<BuildWorkspace.Payload>
{
    public class Payload
    {
        /// <summary>
        /// Name of the workspace to build
        /// </summary>
        public string? WorkspaceName { get; set; }

        /// <summary>
        /// Optional Args
        /// </summary>
        public string? Args { get; set; }

        /// <summary>
        /// Optional discord IP:PORT
        /// </summary>
        public string? DiscordAddress { get; set; }

        /// <summary>
        /// Optional commandIf from the discord server
        /// </summary>
        public ulong CommandId { get; set; }
    }

    public override string Path => "/build";

    protected override async Task<ServerResponse> POST()
    {
        throw new NotSupportedException();
        // await Task.CompletedTask;
        //
        // var args = new Args(Content.Args);
        // args.TryGetArg("-branch", out var branch, "main");
        //
        // var workspaceName = new WorkspaceMapping().GetRemapping(Content.WorkspaceName);
        // var workspace = PlasticWorkspace.GetWorkspaceFromName(workspaceName);
        //
        // if (workspace is null)
        //     return new ServerResponse(
        //         HttpStatusCode.BadRequest,
        //         $"Given namespace is not valid: {Content.WorkspaceName}"
        //     );
        //
        // Logger.Log($"Chosen workspace: {workspace}");
        //
        // workspace.Clear();
        // workspace.Update();
        // workspace.SwitchBranch(branch);
        //
        // var pipeline = App.CreateBuildPipeline(workspace, args);
        // pipeline.Report.OnReportUpdated += OnReportUpdated;
        //
        // App.RunBuildPipe(pipeline).FireAndForget();
        // workspace.GetCurrent(out var changeSetId, out var guid);
        //
        // var data = new BuildPipelineResponse
        // {
        //     ServerVersion = App.ServerVersion,
        //     PipelineId = pipeline.ProjectId,
        //     Workspace = workspace.Name,
        //     WorkspaceMeta = workspace.Meta,
        //     Targets = string.Join(", ", workspace.GetBuildTargets().Select(x => x.Name)),
        //     Args = Content.Args,
        //     UnityVersion = workspace.UnityVersion,
        //     ChangesetId = changeSetId,
        //     ChangesetGuid = guid,
        //     Branch = branch,
        //     ChangesetCount = pipeline.ChangeLog.Length,
        // };
        // return new ServerResponse(HttpStatusCode.OK, data);
    }

    private async void OnReportUpdated(PipelineReport report)
    {
        if (string.IsNullOrEmpty(Content.DiscordAddress))
            return;

        var body = new Dictionary<string, object>
        {
            ["CommandId"] = Content.CommandId,
            ["Report"] = report,
        };
        await Web.SendAsync(
            HttpMethod.Post,
            $"{Content.DiscordAddress}/pipeline-update",
            body: body
        );
    }
}
