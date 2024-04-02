using System.Net;
using Deployment;
using Deployment.Configs;
using Deployment.RemoteBuild;
using SharedLib;
using SharedLib.Build;
using SharedLib.BuildToDiscord;
using SharedLib.Server;

namespace Server.Endpoints;

/// <summary>
/// Builds a specific target on an offload server
/// Class send across network to do remote builds
/// </summary>
public class OffloadBuild : Endpoint<OffloadBuild.Payload>
{
    public class Payload
    {
        public string? SendBackUrl { get; set; }
        public OffloadServerPacket? Packet { get; set; }
    }

    public override string Path => "/offload-build";

    protected override async Task<ServerResponse> POST()
    {
        await Task.CompletedTask;

        if (Content.Packet is null)
            return new ServerResponse(
                HttpStatusCode.BadRequest,
                $"{nameof(Content.Packet)} can not be null"
            );

        var workspaceName = new WorkspaceMapping().GetRemapping(Content.Packet.WorkspaceName);
        var workspace = PlasticWorkspace.GetWorkspaceFromName(workspaceName);

        if (workspace is null)
            return new ServerResponse(
                HttpStatusCode.BadRequest,
                $"Workspace not found: {Content.Packet.WorkspaceName}"
            );

        ProcessAsync(workspace).FireAndForget();
        return ServerResponse.Ok;
    }

    private async Task ProcessAsync(Workspace workspace)
    {
        KeyValuePair<string, OffloadBuildConfig> currentBuildConfig = new();

        try
        {
            // this needs to be here to kick start the thread, otherwise it will stall app
            await Task.Delay(1);

            Environment.CurrentDirectory = workspace.Directory;

            if (Content.Packet.CleanBuild)
                workspace.CleanBuild();

            workspace.Clear();
            workspace.Update();
            workspace.SwitchBranch(Content.Packet.Branch);
            workspace.Update(Content.Packet.ChangesetId);

            if (!Directory.Exists(workspace.Directory))
                throw new DirectoryNotFoundException(
                    $"Directory doesn't exist: {workspace.Directory}"
                );

            // set build version in project settings
            var projWriter = new ProjectSettings(workspace.ProjectSettingsPath);
            projWriter.ReplaceVersions(Content.Packet.BuildVersion);
            workspace.SaveBuildVersion(Content.Packet.BuildVersion.FullVersion);

            foreach (var build in Content.Packet.Builds)
            {
                currentBuildConfig = build;
                var asset = workspace.GetBuildTarget(build.Value.AssetName);
                var deployProcess = GetDeployProcess(build.Value, asset);
                await StartBuilderAsync(
                    Content.Packet.PipelineId,
                    build.Key,
                    asset,
                    workspace,
                    deployProcess
                );
            }

            // clean up after build
            workspace.Clear();
        }
        catch (Exception e)
        {
            Logger.Log(e);

            if (string.IsNullOrEmpty(currentBuildConfig.Key) || currentBuildConfig.Value == null)
                await SendExceptionToMaster(e);
            else
                await SendExceptionToMaster(
                    e,
                    currentBuildConfig.Value.AssetName,
                    currentBuildConfig.Key
                );
        }
        finally
        {
            App.DumpLogs(false);
        }
    }

    /// <summary>
    /// Returns IProcessable for remote deploy if there is one
    /// </summary>
    /// <param name="build"></param>
    /// <param name="asset"></param>
    /// <returns></returns>
    private static IProcessable? GetDeployProcess(
        OffloadBuildConfig build,
        BuildSettingsAsset asset
    )
    {
        if (build.Deploy is null)
            return null;

        var deployStr = Json.Serialise(build.Deploy);

        // if (asset.Target is BuildTarget.iOS)
        // return Json.Deserialise<RemoteAppleDeploy>(deployStr);

        return null;
    }

    /// <summary>
    /// Fire and forget method for starting a build
    /// </summary>
    /// <exception cref="WebException"></exception>
    private async Task StartBuilderAsync(
        string projectId,
        string buildIdGuid,
        BuildSettingsAsset asset,
        Workspace workspace,
        IProcessable? deployProcess = null
    )
    {
        var builder = new UnityBuild(workspace);
        await SendToMasterServerAsync(buildIdGuid, asset.Name, null, BuildTaskStatus.Pending);
        var result = builder.Build(asset);

        if (result.IsErrors)
        {
            await SendToMasterServerAsync(buildIdGuid, asset.Name, result, BuildTaskStatus.Failed);
            return;
        }

        // if no deploy then master server does deploy, need to upload this build
        if (deployProcess is null)
            await Web.StreamToServerAsync(
                $"{Content.SendBackUrl}/upload",
                asset.BuildPath,
                projectId,
                buildIdGuid
            );
        else
            await deployProcess.ProcessAsync();

        // tell master server build is completed
        await SendToMasterServerAsync(
            buildIdGuid,
            asset.Name,
            result,
            result.IsErrors ? BuildTaskStatus.Failed : BuildTaskStatus.Succeed
        );
    }

    private async Task SendToMasterServerAsync(
        string? buildGuid,
        string buildName,
        BuildResult? buildResult,
        BuildTaskStatus status
    )
    {
        var response = new OffloadBuildResponse.Payload
        {
            ProjectId = Content.Packet.PipelineId,
            BuildIdGuid = buildGuid,
            BuildName = buildName,
            BuildResult = buildResult,
            Status = status
        };

        await SendToMasterAsync(response);
    }

    private async Task SendExceptionToMaster(Exception e, string assetName, string buildIdGuid)
    {
        var result = new BuildResult { BuildName = assetName, Errors = new ErrorResponse(e) };

        var response = new OffloadBuildResponse.Payload
        {
            ProjectId = Content.Packet.PipelineId,
            BuildIdGuid = buildIdGuid,
            BuildName = assetName,
            BuildResult = result,
            Status = BuildTaskStatus.Failed
        };

        await SendToMasterAsync(response);
    }

    private async Task SendExceptionToMaster(Exception e)
    {
        var result = new BuildResult { Errors = new ErrorResponse(e) };
        var response = new OffloadBuildResponse.Payload
        {
            ProjectId = Content.Packet.PipelineId,
            BuildResult = result,
            Status = BuildTaskStatus.Failed
        };
        await SendToMasterAsync(response);
    }

    private async Task SendToMasterAsync(OffloadBuildResponse.Payload response)
    {
        var url = $"{Content.SendBackUrl}/offload-response";
        Logger.Log($"Sending build '{response.BuildIdGuid}' back to: {url}");
        await Web.SendAsync(HttpMethod.Post, url, body: response);
    }
}
