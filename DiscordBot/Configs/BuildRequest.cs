using SharedLib;

namespace DiscordBot.Configs;

public class BuildRequest
{
	public WorkspaceReq? WorkspaceBuildRequest { get; set;  }

	public override string ToString()
	{
		return Json.Serialise(this);
	}
}