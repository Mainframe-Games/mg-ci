using System.Net;
using Deployment;
using SharedLib.Server;

namespace Server.Endpoints.PUT;

public class UploadBuild : Endpoint
{
	public override HttpMethod Method => HttpMethod.Put;
	public override string Path => "/upload";
	public override async Task<ServerResponse> ProcessAsync(ListenServer server, HttpListenerContext httpContext)
	{
		await Task.CompletedTask;
		
		if (!httpContext.Request.HasEntityBody) return ServerResponse.NoContent;

		// get pipeline
		var pipelineId = ulong.Parse(httpContext.Request.Headers["pipelineId"] ?? "0");
		if (!App.Pipelines.TryGetValue(pipelineId, out var buildPipeline))
			return new ServerResponse(HttpStatusCode.NotFound, $"{nameof(BuildPipeline)} not found with ID: {pipelineId}");

		// create file
		var fileName = httpContext.Request.Headers["fileName"] ?? string.Empty;
		var buildPath = httpContext.Request.Headers["buildPath"] ?? string.Empty;
		var path = System.IO.Path.Combine(buildPipeline.Workspace.Directory, buildPath, fileName);
		var fileInfo = new FileInfo(path);
		fileInfo.Directory?.Create();

		// write to file
		await using var fs = fileInfo.Create();
		await httpContext.Request.InputStream.CopyToAsync(fs);
		return new ServerResponse(HttpStatusCode.OK, "File uploaded successfully.");
	}
}