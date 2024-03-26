using System.Net;
using Deployment.RemoteBuild;
using SharedLib;
using SharedLib.BuildToDiscord;
using SharedLib.Server;
using WebSocketSharp;
using WebSocketSharp.Server;
using Logger = SharedLib.Logger;

namespace Server.Services;

public class BuildService : WebSocketBehavior
{
    public class Payload
    {
        public _Plastic? Plastic { get; set; }
        public _Git? Git { get; set; }
        public _Discord? Discord { get; set; }
        
        /// <summary>
        /// Optional Args
        /// </summary>
        public string? Args { get; set; }
        
        public class _Git
        {
            public string? Url { get; set; }
        }
        
        public class _Plastic
        {
            /// <summary>
            /// Name of the workspace to build
            /// </summary>
            public string? WorkspaceName { get; set; }
        }

        public class _Discord
        {
            /// <summary>
            /// Optional discord IP:PORT
            /// </summary>
            public string? DiscordAddress { get; set; }
		    
            /// <summary>
            /// Optional commandId from the discord server
            /// </summary>
            public ulong CommandId { get; set; }
        }
    }
    
    protected override void OnMessage(MessageEventArgs e)
    {
        base.OnMessage(e);
        
        var json = Json.Deserialise<Payload>(e.Data);

        ServerResponse? response = null;
        
        if (json?.Plastic is not null)
        {
            response = StartBuildPlastic(json);
        }
        else
        {
          // implement git   
        }
        
        if (response is null)
            return;
        
        Send(Json.Serialise(response));
    }
    
    private void OnReportUpdated(PipelineReport report)
    {
        var body = new Dictionary<string, object>
        {
            // ["CommandId"] = Content.CommandId,
            ["Report"] = report,
        };
        
        Sessions.Broadcast(Json.Serialise(body));
    }

    private ServerResponse StartBuildPlastic(Payload payload)
    {
        if (payload.Plastic is null)
            return new ServerResponse(HttpStatusCode.BadRequest, "Payload is null");
        
        var args = new Args(payload.Args);
        args.TryGetArg("-branch", out var branch, "main");

        var workspaceName = new WorkspaceMapping().GetRemapping(payload.Plastic.WorkspaceName);
        var workspace = PlasticWorkspace.GetWorkspaceFromName(workspaceName);
		
        if (workspace is null)
            return new ServerResponse(HttpStatusCode.BadRequest, $"Given namespace is not valid: {payload.Plastic.WorkspaceName}");
		
        Logger.Log($"Chosen workspace: {workspace}");
		
        workspace.Clear();
        workspace.Update();
        workspace.SwitchBranch(branch);

        var pipeline = App.CreateBuildPipeline(workspace, args);
        pipeline.Report.OnReportUpdated += OnReportUpdated;
		
        App.RunBuildPipe(pipeline).FireAndForget();
        workspace.GetCurrent(out var changeSetId, out var guid);

        var data = new BuildPipelineResponse
        {
            ServerVersion = App.Version,
            PipelineId = pipeline.Id,
            Workspace = workspace.Name,
            WorkspaceMeta = workspace.Meta,
            Targets = string.Join(", ", workspace.GetBuildTargets().Select(x => x.Name)),
            Args = payload.Args,
            UnityVersion = workspace.UnityVersion,
            ChangesetId = changeSetId,
            ChangesetGuid = guid,
            Branch = branch,
            ChangesetCount = pipeline.ChangeLog.Length,
        };
        return new ServerResponse(HttpStatusCode.OK, data);
    }
}