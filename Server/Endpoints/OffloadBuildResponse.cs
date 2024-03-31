using System.Net;
using Deployment;
using Deployment.Configs;
using SharedLib;
using SharedLib.BuildToDiscord;
using SharedLib.Server;

namespace Server.Endpoints;

/// <summary>
/// Response from offload server, used on master server
/// </summary>
public class OffloadBuildResponse : Endpoint<OffloadBuildResponse.Payload>
{
    public class Payload
    {
        public string? ProjectId { get; set; }
        public string? BuildIdGuid { get; set; }
        public string? BuildName { get; set; }
        public BuildTaskStatus? Status { get; set; }
        public BuildResult? BuildResult { get; set; }
    }

    public override string Path => "/offload-response";

    protected override async Task<ServerResponse> POST()
    {
        await Task.CompletedTask;

        if (!App.Pipelines.TryGetValue(Content.ProjectId, out var buildPipeline))
            return LogAndReturn(
                new ServerResponse(
                    HttpStatusCode.BadRequest,
                    $"{nameof(UnityBuildPipeline)} is not active. Id: {Content.ProjectId}"
                )
            );

        // if build name or buildGUID is null then errors could of happened before builds could even start
        if (Content.BuildName == null || Content.BuildIdGuid == null)
        {
            buildPipeline.SendErrorHook(
                new Exception(
                    Content.BuildResult?.Errors?.ToString()
                        ?? "Unknown error. Something went wrong with offload server"
                )
            );
            return ServerResponse.Ok;
        }

        if (
            Content.BuildResult == null
            && Content.Status is BuildTaskStatus.Succeed or BuildTaskStatus.Failed
        )
            return LogAndReturn(
                new ServerResponse(
                    HttpStatusCode.BadRequest,
                    $"{nameof(BuildResult)} can not be null"
                )
            );

        buildPipeline.SetOffloadBuildStatus(
            Content.BuildIdGuid,
            Content.BuildName,
            Content.Status ?? default,
            Content.BuildResult
        );
        return ServerResponse.Ok;
    }

    private static ServerResponse LogAndReturn(ServerResponse res)
    {
        Logger.Log(res);
        return res;
    }
}
