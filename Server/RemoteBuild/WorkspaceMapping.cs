using SharedLib;

namespace Deployment.RemoteBuild;

public class WorkspaceMapping
{
	private static string ConfigPath => Args.Environment.TryGetArg("-mapping", out var mappingPath)
		? mappingPath
		: "workspacemapping.json";

	private Dictionary<string, string> Mapping { get; } = new();

	public WorkspaceMapping()
	{
		if (!File.Exists(ConfigPath))
			return;

		var json = File.ReadAllText(ConfigPath);
		Mapping = Json.Deserialise<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
	}

	public string GetRemapping(string? workspaceName)
	{
		if (workspaceName == null)
			throw new NullReferenceException($"{nameof(workspaceName)} param is null");

		if (!Mapping.TryGetValue(workspaceName, out var remappingName))
			return workspaceName;

		Logger.Log($"Remapping Workspace name: {workspaceName} -> {remappingName}");
		return remappingName;
	}
}