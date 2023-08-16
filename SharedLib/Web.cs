using System.Diagnostics;
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

	public static async Task StreamToServerAsync(string? url, string buildPath, ulong pipelineId, string buildIdGuid)
	{
		using var client = new HttpClient();
		var rootDir = new DirectoryInfo(buildPath);
		
		client.DefaultRequestHeaders.Add("buildPath", buildPath);
		client.DefaultRequestHeaders.Add(nameof(pipelineId), pipelineId.ToString());
		client.DefaultRequestHeaders.Add(nameof(buildIdGuid), buildIdGuid);

		var sw = Stopwatch.StartNew();
		var totalBytes = rootDir.GetByteSize();
		Logger.Log($"Uploading contents... {buildPath} ({totalBytes.ToByteSizeString()})");
		
		var progressBar = new ProgressBar();
		TotalUploadBytes = totalBytes;
		CurrentUploadBytes = 0;
		
		await UploadDirectoryAsync(client, url, rootDir, rootDir.FullName, progressBar);
		
		progressBar.Dispose();
		Logger.LogTimeStamp("Upload complete:", sw);
	}

	// TODO: could pass this in as params but this is fine for now
	private static ulong CurrentUploadBytes;
	private static ulong TotalUploadBytes;

	private static async Task UploadDirectoryAsync(HttpClient client, string? url, DirectoryInfo directoryInfo, string rootDirPath, ProgressBar progressBar)
	{
		foreach (var file in directoryInfo.GetFiles())
		{
			await using var fs = file.OpenRead();
			var bytes = await ReadFully(fs);
			
			// add fileName as header
			var fileLocalName = file.FullName
				.Replace(rootDirPath, string.Empty)
				.Replace('\\', '/')
				.Trim('/');
			client.DefaultRequestHeaders.Remove("fileName");
			client.DefaultRequestHeaders.Add("fileName", fileLocalName);
			
			// log progress
			CurrentUploadBytes += (ulong)bytes.Length;
			progressBar.SetContext($"{CurrentUploadBytes.ToByteSizeString()}/{TotalUploadBytes.ToByteSizeString()} | Uploading: {fileLocalName} ({bytes.ToByteSizeString()})");
			progressBar.Report(CurrentUploadBytes / (double)TotalUploadBytes);
			
			// upload
			var content = new ByteArrayContent(bytes);
			var res = await client.PutAsync(url, content);

			// Check response status if needed
			if (!res.IsSuccessStatusCode)
				throw new WebException($"Failed with code: {res.StatusCode}. Reason: {res.ReasonPhrase}");
		}

		foreach (var subDir in directoryInfo.GetDirectories())
			await UploadDirectoryAsync(client, url, subDir, rootDirPath, progressBar);
	}

	private static async Task<byte[]> ReadFully(Stream stream)
	{
		using var ms = new MemoryStream();
		await stream.CopyToAsync(ms);
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