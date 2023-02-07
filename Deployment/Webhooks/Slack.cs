using System.Collections.Specialized;
using System.Net;
using System.Text;
using Newtonsoft.Json.Linq;
using SharedLib;

namespace Deployment.Webhooks;

public static class Slack
{
	/// <summary>
	/// Post a message using a Payload object
	/// </summary>
	public static void PostMessage(string channelHook, string message)
	{
		try
		{
			var payloadJson = new JObject { ["text"] = message }.ToString();
			using var client = new WebClient();
			var data = new NameValueCollection
			{
				["payload"] = payloadJson
			};

			var url = new Uri(channelHook);
			var response = client.UploadValues(url, "POST", data);
			var responseText = Encoding.UTF8.GetString(response);
			Logger.Log(responseText);
		}
		catch (Exception e)
		{
			Console.Error.WriteLine($"Slack hack error with url: {channelHook}");
			Console.Error.WriteLine(e);
		}
	}
}