using System.Net;

namespace SharedLib.Server;

public class ErrorResponse
{
	public string? ErrorCode { get; set; } = HttpStatusCode.InternalServerError.ToString();
	public string? Exception { get; set; }
	public string? Message { get; set; }
	public string?[]? StackTrace { get; set; }

	public ErrorResponse()
	{
	}

	public ErrorResponse(Exception e)
	{
		Exception = e.GetType().Name;
		Message = e.Message;
		StackTrace = e.StackTrace?.Split(Environment.NewLine).Select(x => x.Trim()).ToArray();
	}

	public override string ToString()
	{
		return Json.Serialise(this);
	}
}