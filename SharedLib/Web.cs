using System.Net;
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
		Logger.Log($"Sending Data: {data.ToByteSizeString()}");
		using var res = await client.PutAsync(url, byteContent);
		return await GetSuccess(res);
	}

	public static async Task<Response> StreamToServerAsync(string? url, string dirPath, ulong pipelineId, string buildIdGuid)
	{
		using var client = new HttpClient();
		var rootDir = new DirectoryInfo(dirPath);
		
		// client.DefaultRequestHeaders.Add(nameof(dirPath), dirPath);
		client.DefaultRequestHeaders.Add(nameof(pipelineId), pipelineId.ToString());
		client.DefaultRequestHeaders.Add(nameof(buildIdGuid), buildIdGuid);

		var res = await UploadDirectoryAsync(client, url, rootDir, rootDir.FullName);
		return await GetSuccess(res);
	}

	private static async Task<HttpResponseMessage> UploadDirectoryAsync(HttpClient client, string? url, DirectoryInfo directoryInfo, string rootDirPath)
	{
		HttpResponseMessage? res = null;
		
		foreach (var file in directoryInfo.GetFiles())
		{
			await using var fs = file.OpenRead();
			var fileContent = await ReadFully(fs, rootDirPath);
			var content = new ByteArrayContent(fileContent);
			res = await client.PutAsync(url, content);

			// Check response status if needed
			if (!res.IsSuccessStatusCode)
				throw new WebException($"Failed with code: {res.StatusCode}. Reason: {res.ReasonPhrase}");
		}

		foreach (var subDir in directoryInfo.GetDirectories())
			await UploadDirectoryAsync(client, url, subDir, rootDirPath);

		return res;
	}

	private static async Task<byte[]> ReadFully(FileStream fileStream, string rootDirPath)
	{
		using var ms = new MemoryStream();
		await using var writer = new BinaryWriter(ms);
		var fileLocalName = fileStream.Name.Replace(rootDirPath, string.Empty).Trim('\\');
		writer.Write(fileLocalName);
		await fileStream.CopyToAsync(ms);
		return ms.ToArray();
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