using System.Net;
using Deployment;
using SharedLib.Server;

namespace Server.Endpoints;

public class UploadBuild : Endpoint<object>
{
	public override string Path => "/upload";

	protected override async Task<ServerResponse> PUT()
	{
		await Task.CompletedTask;

		if (!HttpContext.Request.HasEntityBody)
			return ServerResponse.NoContent;

		// get pipeline
		var pipelineId = ulong.Parse(HttpContext.Request.Headers["pipelineId"] ?? "0");
		if (!App.Pipelines.TryGetValue(pipelineId, out var buildPipeline))
			return new ServerResponse(HttpStatusCode.NotFound, $"{nameof(BuildPipeline)} not found with ID: {pipelineId}");

		// create file
		var fileName = HttpContext.Request.Headers["fileName"] ?? string.Empty;
		var buildPath = HttpContext.Request.Headers["buildPath"] ?? string.Empty;
		var path = System.IO.Path.Combine(buildPipeline.Workspace.Directory, buildPath, fileName);
		var fileInfo = new FileInfo(path);
		fileInfo.Directory?.Create();

		// write to file
		await using var fs = fileInfo.Create();
		await HttpContext.Request.InputStream.CopyToAsync(fs);
		return new ServerResponse(HttpStatusCode.OK, "File uploaded successfully.");
	}
}