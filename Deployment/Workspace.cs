using Deployment.Misc;

namespace Deployment;

public readonly struct Workspace
{
	public string Name { get; private init; }
	public string Directory { get; private init; }
	public string UnityVersion { get; private init; }

	public override string ToString()
	{
		return $"{Name} @ {Directory} | UnityVersion: {UnityVersion}";
	}

	private static List<Workspace> GetAvailableWorkspaces()
	{
		var (exitCode, output) = Cmd.Run("cm", "workspace", false);

		if (exitCode != 0)
			throw new Exception(output);

		var workSpacesArray = output.Split(Environment.NewLine);
		var workspaces = new List<Workspace>();

		for (int i = 0; i < workSpacesArray.Length; i++)
		{
			var split = workSpacesArray[i].Split('@');
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
		var args = Environment.GetCommandLineArgs();
		if (args.Contains("-config"))
			return GetCustomWorkspace();

		var (exitCode, output) = Cmd.Run("cm", "workspace", false);

		if (exitCode != 0)
			throw new Exception(output);

		var workspaces = GetAvailableWorkspaces();
		var index = Cmd.Choose("Choose workspace", workspaces.Select(x => x.Name).ToList());
		return workspaces[index];
	}

	public static Workspace GetWorkspaceFromName(string workspaceName)
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

	private static string GetUnityVersion(string workingDirectory)
	{
		var path = Path.Combine(workingDirectory, "ProjectSettings", "ProjectVersion.txt");
		var txt = File.ReadAllText(path);
		var firstLine = txt.Split("\n")[0];
		var version = firstLine.Replace("m_EditorVersion:", string.Empty).Trim();
		return version;
	}
}