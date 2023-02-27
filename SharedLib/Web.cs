using System.Net;
using System.Text;

namespace SharedLib;

public static class Web
{
	public struct Response
	{
		public HttpStatusCode StatusCode;
		public string Reason;
		public string Content;
	}

	public static async Task<Response> SendAsync(
		HttpMethod method,
		string? url,
		string? authToken = null,
		object? body = null,
		params (HttpRequestHeader key, string value)[] headers)
	{
		if (url == null)
			throw new NullReferenceException("Url can not be null");

		try
		{
			using var client = new HttpClient();
			var msg = new HttpRequestMessage
			{
				Method = method,
				RequestUri = new Uri(url),
				Headers =
				{
					{ HttpRequestHeader.Accept.ToString(), "application/json" },
				}
			};

			if (!string.IsNullOrEmpty(authToken))
				msg.Headers.Add(HttpRequestHeader.Authorization.ToString(), authToken);

			foreach (var header in headers)
				msg.Headers.Add(header.key.ToString(), header.value);

			var jsonBody = string.Empty;
			
			if (body != null)
			{
				jsonBody = Json.Serialise(body);
				msg.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
			}

			var jsonBodyLog = jsonBody.Length > 1000 ? $"{jsonBody[..1000]}...[truncated]" : jsonBody;
			Logger.Log($"HTTP {method.ToString().ToUpper()} {url}\n{jsonBodyLog}");
			
			var res = await client.SendAsync(msg);
			var content = await res.Content.ReadAsStringAsync();
			
			Logger.Log($"{res.StatusCode} {content}");

			return new Response
			{
				StatusCode = res.StatusCode,
				Reason = res.ReasonPhrase ?? string.Empty,
				Content = content
			};
		}
		catch (Exception e)
		{
			return new Response
			{
				StatusCode = HttpStatusCode.InternalServerError,
				Reason = e.Message
			};
		}
	}
}