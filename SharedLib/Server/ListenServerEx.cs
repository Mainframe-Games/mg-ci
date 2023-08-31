using System.Net;

namespace SharedLib.Server;

public static class ListenServerEx
{
	public static async Task<T> GetPostContentAsync<T>(this HttpListenerContext context)
	{
		using var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding);
		var jsonStr = await reader.ReadToEndAsync();
		var packet = Json.Deserialise<T>(jsonStr);
		
		if (packet is null)
			throw new NullReferenceException($"{typeof(T).Name} is null from json: {jsonStr}");

		return packet;
	}
}