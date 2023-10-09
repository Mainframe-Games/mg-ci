using System.Net;

namespace SharedLib.Server;

public class ErrorResponse
{
	public HttpStatusCode? Code { get; set; } = HttpStatusCode.InternalServerError;
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
		StackTrace = ParseStackTrace(e.StackTrace);
	}

	public static string?[]? ParseStackTrace(string? stackTrace)
	{
		return stackTrace?
			.Split(Environment.NewLine)
			.Select(x => x.Trim())
			.ToArray();
	}

	public override string ToString()
	{
		return Json.Serialise(this);
	}
}