namespace SharedLib;

public class Workspace
{
	public string? Name { get; private init; }
	public string? Directory { get; private init; }
	public string? UnityVersion { get; private set; }

	public override string ToString()
	{
		return $"{Name} @ {Directory} | UnityVersion: {UnityVersion}";
	}
	
	public static List<Workspace> GetAvailableWorkspaces()
	{
		var (exitCode, output) = Cmd.Run("cm", "workspace", false);

		if (exitCode != 0)
			throw new Exception(output);

		var workSpacesArray = output.Split(Environment.NewLine);
		var workspaces = new List<Workspace>();

		foreach (var workspace in workSpacesArray)
		{
			var split = workspace.Split('@');
			var name = split[0];
			var path = split[1].Replace(Environment.MachineName, string.Empty).Trim();
			var unityVersion = GetUnityVersion(path);
			var ws = new Workspace
			{
				Name = name,
				Directory = path,
				UnityVersion = unityVersion
			};
			workspaces.Add(ws);
		}

		return workspaces;
	}

	public static Workspace GetWorkspace()
	{
		//if (Args.Environment.IsFlag("-config"))
		//	return GetCustomWorkspace();

		var (exitCode, output) = Cmd.Run("cm", "workspace", false);

		if (exitCode != 0)
			throw new Exception(output);

		var workspaces = GetAvailableWorkspaces();
		var workspaceNames = workspaces.Select(x => x.Name).ToList();
		var index = Cmd.Choose("Choose workspace", workspaceNames);
		return workspaces[index];
	}

	public static Workspace GetWorkspaceFromName(string? workspaceName)
	{
		var workspaces = GetAvailableWorkspaces();
		return workspaces.First(x => x.Name == workspaceName);
	}

	private static Workspace GetCustomWorkspace()
	{
		var args = Environment.GetCommandLineArgs();
		var index = Array.IndexOf(args, "-config");
		var path = args[index + 1].Trim();
		var workspaceName = path.Split(Path.DirectorySeparatorChar)[^1];
		var unityVersion = GetUnityVersion(path);
		return new Workspace
		{
			Name = workspaceName,
			Directory = path,
			UnityVersion = unityVersion
		};
	}

	private static string GetUnityVersion(string? workingDirectory)
	{
		if (workingDirectory == null)
			return string.Empty;
		
		var path = Path.Combine(workingDirectory, "ProjectSettings", "ProjectVersion.txt");
		var txt = File.ReadAllText(path);
		var lines = txt.Split("\n");
		
		foreach (var line in lines)
		{
			if (line.Contains("m_EditorVersion:"))
				return line.Replace("m_EditorVersion:", string.Empty).Trim();
		}

		return string.Empty;
	}
	
	public void Clear()
	{
		Cmd.Run("cm", $"unco -a \"{Directory}\"");
		UnityVersion = GetUnityVersion(Directory);
	}

	/// <summary>
	/// Updates the workspace. 
	/// </summary>
	/// <param name="changeSetId">ChangeSetId to update to. -1 is latest</param>
	public void Update(int changeSetId = -1)
	{
		// get all the latest updates
		Cmd.Run("cm", $"update \"{Directory}\"");
		
		// set to a specific change set
		if (changeSetId > 0)
			Cmd.Run("cm", $"switch cs:{changeSetId} --workspace=\"{Directory}\"");
		
		UnityVersion = GetUnityVersion(Directory);
	}
}