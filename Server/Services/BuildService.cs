using System.Net;
using AvaloniaAppMVVM.Data;
using Deployment.RemoteBuild;
using LibGit2Sharp;
using ServerClientShared;
using SharedLib;
using SharedLib.BuildToDiscord;
using SharedLib.Server;
using Logger = SharedLib.Logger;

namespace Server.Services;

public class BuildService : ServiceBase
{
    protected override void OnMessage(NetworkPayload payload)
    {
        switch (payload.Type)
        {
            case MessageType.Connection:
                break;
            case MessageType.Disconnection:
                break;

            case MessageType.Message:

                var project =
                    Json.Deserialise<Project>(payload.Data?.ToString())
                    ?? throw new NullReferenceException();

                StartBuild(project);

                break;

            case MessageType.Error:
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

        var response = project.Settings.VersionControl switch
        {
            VersionControlType.Git => StartGitBuild(project),
            VersionControlType.Plastic => StartBuildPlastic(project),
            _ => throw new ArgumentOutOfRangeException()
        };

        Send(new NetworkPayload(MessageType.Message, 0, response));
    }

    private static ServerResponse StartGitBuild(Project project)
    {
        var projectRoot = new DirectoryInfo(
            Path.Combine(App.CiCachePath, project.Settings.ProjectName!)
        );

        // clone
        Git.Clone(
            project.Settings.GitRepositoryUrl!,
            projectRoot.FullName,
            project.Settings.Branch!
        );

        // get to build location
        var projWorkingPath = Path.Combine(
            projectRoot.FullName,
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

        var workspace = new GitWorkspace(
            new Repository(projectRoot.FullName),
            project.Settings.ProjectName!,
            buildLocation.FullName,
            project.Guid!
        );

        // set credentials
        var gitUser = App.Config.Git.Username;
        var accessToken = App.Config.Git.AccessToken;
        workspace.SetCredentials(gitUser, accessToken);

        // run build
        var pipeline = new ServerPipeline(project, workspace);
        pipeline.Run();

        return ServerResponse.Ok;
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
        PrepareWorkspace(workspace, project);

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
            UnityVersion = workspace.UnityVersion,
            ChangesetId = changeSetId,
            ChangesetGuid = guid,
            Branch = project.Settings.Branch,
            ChangesetCount = pipeline.ChangeLog.Length,
        };
        return new ServerResponse(HttpStatusCode.OK, data);
    }

    private static void PrepareWorkspace(Workspace workspace, Project project)
    {
        workspace.Clear();
        workspace.Update();
        workspace.SwitchBranch(project.Settings.Branch!);
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
