using System.Net;
using AvaloniaAppMVVM.Data;
using Deployment.RemoteBuild;
using LibGit2Sharp;
using ServerClientShared;
using SharedLib;
using SharedLib.BuildToDiscord;
using SharedLib.Server;
using WebSocketSharp;
using Logger = SharedLib.Logger;

namespace Server.Services;

public class BuildService : ServiceBase
{
    protected override void OnMessage(MessageEventArgs e)
    {
        base.OnMessage(e);

        Console.WriteLine($"/build: {e.Data}");
        var json = Json.Deserialise<NetworkPayload>(e.Data) ?? throw new NullReferenceException();

        switch (json.Type)
        {
            case MessageType.Connection:
                break;
            case MessageType.Disconnection:
                break;
            case MessageType.Message:
                var project =
                    Json.Deserialise<Project>(json.Data?.ToString())
                    ?? throw new NullReferenceException();

                StartBuild(project);

                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void StartBuild(Project project)
    {
        if (App.Pipelines.ContainsKey(project.Guid!))
        {
            Send(
                new NetworkPayload(MessageType.Error, 0, $"Pipeline already exists: {project.Guid}")
            );
            return;
        }

        ServerResponse? response = null;

        switch (project.Settings.VersionControl)
        {
            case VersionControlType.Git:
                response = StartGitBuild(project);

                break;
            case VersionControlType.Plastic:
                response = StartBuildPlastic(project);
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }

        Send(new NetworkPayload(MessageType.Error, 0, response));
    }

    private ServerResponse? StartGitBuild(Project project)
    {
        // clone
        var homeFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var path = new DirectoryInfo(
            Path.Combine(homeFolder, "ci-cache", project.Settings.ProjectName!)
        );
        var dir = Git.Clone(
            project.Settings.GitRepositoryUrl!,
            path.FullName,
            project.Settings.Branch!
        );

        // get to build location
        var projWorkingPath = Path.Combine(
            path.FullName,
            project.Settings.GitRepositorySubPath?.TrimStart('/')?.TrimStart('\\') ?? string.Empty
        );
        var buildLocation = new DirectoryInfo(projWorkingPath);

        if (!buildLocation.Exists)
        {
            return new ServerResponse(
                HttpStatusCode.BadRequest,
                $"Build location does not exist: {buildLocation.FullName}"
            );
        }

        var repo = new Repository(dir);
        var workspace = new GitWorkspace(
            repo,
            project.Settings.ProjectName!,
            buildLocation.FullName,
            project.Guid!
        );

        var res = StartBuildPipeline(workspace, project);
        return res;
    }

    private ServerResponse StartBuildPlastic(Project project)
    {
        var workspaceName = new WorkspaceMapping().GetRemapping(project.Settings.ProjectName);
        var workspace = PlasticWorkspace.GetWorkspaceFromName(workspaceName);

        if (workspace is null)
            return new ServerResponse(
                HttpStatusCode.BadRequest,
                $"Given namespace is not valid: {project.Settings.ProjectName}"
            );

        Logger.Log($"Chosen workspace: {workspace}");
        var res = StartBuildPipeline(workspace, project);
        return res;
    }

    private ServerResponse StartBuildPipeline(Workspace workspace, Project project)
    {
        workspace.Clear();
        workspace.Update();
        workspace.SwitchBranch(project.Settings.Branch!);

        var args = new Args(""); // TODO: remove args from pipeline, everything should be done in C# classes
        var pipeline = App.CreateBuildPipeline(workspace, args, project);
        pipeline.Report.OnReportUpdated += OnReportUpdated;

        App.RunBuildPipe(pipeline).FireAndForget();
        workspace.GetCurrent(out var changeSetId, out var guid);

        var data = new BuildPipelineResponse
        {
            ServerVersion = App.ServerVersion,
            PipelineId = pipeline.ProjectId,
            Workspace = workspace.Name,
            WorkspaceMeta = workspace.Meta,
            Targets = string.Join(", ", workspace.GetBuildTargets().Select(x => x.Name)),
            Args = args.ToString(),
            UnityVersion = workspace.UnityVersion,
            ChangesetId = changeSetId,
            ChangesetGuid = guid,
            Branch = project.Settings.Branch,
            ChangesetCount = pipeline.ChangeLog.Length,
        };
        return new ServerResponse(HttpStatusCode.OK, data);
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
}
