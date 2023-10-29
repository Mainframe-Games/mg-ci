using System.Net;
using Server.RemoteBuild;
using SharedLib;
using SharedLib.Server;

namespace Server.Endpoints;

/// <summary>
/// For updating game servers
/// </summary>
public class GameServerUpdate : Endpoint<GameServerUpdate.Payload>
{
	public class Payload
	{
		public MultiplayServerUpdate? Usg { get; set; }
		public ClanforgeImageUpdate? Clanforge { get; set; }
	}

	public override string Path => "/game-server-update";
	protected override async Task<ServerResponse> POST()
	{
		await Task.CompletedTask;
		if (Content.Usg != null) return await Content.Usg.ProcessAsync();
		if (Content.Clanforge != null) return await Content.Clanforge.ProcessAsync();
		return new ServerResponse(HttpStatusCode.BadRequest, $"Issue with {nameof(GameServerUpdate)}. {Json.Serialise(this)}");
	}
}