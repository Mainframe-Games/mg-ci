using System.Net;
using Server.RemoteBuild;
using SharedLib;
using SharedLib.Server;

namespace Server.Endpoints.POST;

/// <summary>
/// For updating game servers
/// </summary>
public class GameServerUpdate : EndpointPOST<GameServerUpdate.Payload>
{
	public class Payload
	{
		public MultiplayServerUpdate? Usg { get; set; }
		public RemoteClanforgeImageUpdate? Clanforge { get; set; }
	}

	public override HttpMethod Method => HttpMethod.Post;
	public override string Path => "/game-server-update";
	public override async Task<ServerResponse> ProcessAsync(ListenServer server, HttpListenerContext httpContext, Payload content)
	{
		await Task.CompletedTask;
		if (content.Usg != null) return content.Usg.Process();
		if (content.Clanforge != null) return content.Clanforge.Process();
		return new ServerResponse(HttpStatusCode.BadRequest, $"Issue with {nameof(GameServerUpdate)}. {Json.Serialise(this)}");
	}
}