﻿using System.Net;
using System.Text;

namespace SharedLib;

public static class Web
{
	public struct Response
	{
		public HttpStatusCode StatusCode;
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
		
		using var res = await client.SendAsync(msg);
		
		if (!res.IsSuccessStatusCode)
			throw new WebException($"Failed with code: {res.StatusCode}. Reason: {res.ReasonPhrase}");
		
		return await GetSuccess(res);
	}

	public static async Task<Response> SendBytesAsync(string? url, byte[] data)
	{
		using var client = new HttpClient();
		using var byteContent = new ByteArrayContent(data);
		Logger.Log($"Sending Data: {data.ToMegaByteString()}");
		using var res = await client.PutAsync(url, byteContent);
		return await GetSuccess(res);
	}

	private static async Task<Response> GetSuccess(HttpResponseMessage res)
	{
		var content = await res.Content.ReadAsStringAsync();

		Logger.Log($"Web Response ({res.StatusCode}): {content}");

		return new Response
		{
			StatusCode = res.StatusCode,
			Content = content
		};
	}
}