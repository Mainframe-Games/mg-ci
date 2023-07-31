namespace SharedLib;

/// <summary>
/// Workspace packet used to send across network.
/// </summary>
public class WorkspacePacket
{
	public string? Name { get; set; }
	public List<string>? Targets { get; set; }
	
	public static List<WorkspacePacket> GetFromLocal()
	{
		return Workspace.GetAvailableWorkspaces()
			.Select(w => new WorkspacePacket
			{
				Name = w.Name,
				Targets = w.GetBuildTargets()
					.Select(t => t.Name)
					.ToList()
			})
			.ToList();
	}
}