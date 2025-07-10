﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Newtonsoft.Json.Linq;

namespace SharedLib.Webhooks;

public static class Discord
{
	public enum Colour
	{
		DEFAULT = 0,
		AQUA = 1752220,
		DARK_AQUA = 1146986,
		GREEN = 3066993,
		DARK_GREEN = 2067276,
		BLUE = 3447003,
		DARK_BLUE = 2123412,
		PURPLE = 10181046,
		DARK_PURPLE = 7419530,
		LUMINOUS_VIVID_PINK = 15277667,
		DARK_VIVID_PINK = 11342935,
		GOLD = 15844367,
		DARK_GOLD = 12745742,
		ORANGE = 15105570,
		DARK_ORANGE = 11027200,
		RED = 15158332,
		DARK_RED = 10038562,
		GREY = 9807270,
		DARK_GREY = 9936031,
		DARKER_GREY = 8359053,
		LIGHT_GREY = 12370112,
		NAVY = 3426654,
		DARK_NAVY = 2899536,
		YELLOW = 16776960,
	}

	/// <summary>
	/// Src: https://discohook.org/
	/// </summary>
	public struct Embed
	{
		public string? AuthorName;
		public string? AuthorIconUrl;
		public string? Url;
		public string? ThumbnailUrl;
		public string? Username;
		public string? Title;
		public string? Description;
		public Colour? Colour;
		public List<Field>? Fields;
		public bool? IncludeTimeStamp;

		public JObject BuildJson()
		{
			return new JObject
			{
				["url"] = Url,
				["thumbnail"] = new JObject { ["url"] = ThumbnailUrl },
				["title"] = Title,
				["color"] = ((int)(Colour ?? Discord.Colour.DEFAULT)).ToString(),
				["description"] = Description,
			};
		}
	}

	public struct Field
	{
		public string? Name;
		public object? Value;
	}

	public static void PostMessage(string channelUrl, Embed embed)
	{
		try
		{
			// send change long to discord
			var req = (HttpWebRequest)WebRequest.Create(channelUrl);
			req.ContentType = "application/json";
			req.Method = "POST";

			var json = new JObject
			{
				["username"] = embed.Username,
				["embeds"] = new JArray(embed.BuildJson())
			};

			var data = Encoding.ASCII.GetBytes(json.ToString());
			req.ContentLength = data.Length;
			using var stream = req.GetRequestStream();
			stream.Write(data, 0, data.Length);
			using var res = req.GetResponse();
			// discord doesn't respond with any text so just fire and forget
		}
		catch (Exception e)
		{
			Logger.Log(e);
		}
	}
	
	public static void PostMessage(string channelUrl, string message, string username, string title, Colour colour = Colour.DEFAULT)
	{
		try
		{
			// send change long to discord
			var req = (HttpWebRequest)WebRequest.Create(channelUrl);
			req.ContentType = "application/json";
			req.Method = "POST";

			var json = new JObject
			{
				["username"] = username,
				["embeds"] = new JArray(new JObject
				{
					["title"] = title,
					["color"] = ((int)colour).ToString(),
					["description"] = message,
				})
			};

			var data = Encoding.ASCII.GetBytes(json.ToString());
			req.ContentLength = data.Length;
			using var stream = req.GetRequestStream();
			stream.Write(data, 0, data.Length);
			using var res = req.GetResponse();
			// discord doesn't respond with any text so just fire and forget
		}
		catch (Exception e)
		{
			Logger.Log(e);
		}
	}
}