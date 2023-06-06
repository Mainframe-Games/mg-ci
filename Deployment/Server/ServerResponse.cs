using System.Net;

namespace Deployment.Server;

public class ServerResponse
{
	public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;
	public ulong? PipelineId { get; set; }
	public string? Message { get; set; }
	public int? ChangesetId { get; set; }
	public string? ChangesetGuid { get; set; }
	public string? Branch { get; set; }
	public string? UnityVersion { get; set; }

	public ServerResponse() {}
	
	public ServerResponse(HttpStatusCode statusCode, string? message)
	{
		StatusCode = statusCode;
		Message = message;
	}

	public static readonly ServerResponse Default = new();
}