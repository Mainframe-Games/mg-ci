using SharedLib;

namespace Deployment.RemoteBuild;

public class WorkspaceMapping
{
	private static string ConfigPath => Args.Environment.TryGetArg("-mapping", out var mappingPath)
		? mappingPath
		: "workspacemapping.json";

	private Dictionary<string, string> Mapping { get; }

	public WorkspaceMapping()
	{
		if (File.Exists(ConfigPath))
		{
			var json = File.ReadAllText(ConfigPath);
			Mapping = Json.Deserialise<Dictionary<string, string>>(json) ?? new();
		}
		else
		{
			Mapping = new();
		}
	}

	public string GetRemapping(string? workspaceName)
	{
		if (workspaceName == null)
			throw new NullReferenceException($"{nameof(workspaceName)} param is null");
		
		return Mapping.TryGetValue(workspaceName, out var remappingName) ? remappingName : workspaceName;
	}
}