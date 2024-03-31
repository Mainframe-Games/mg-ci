namespace UnityBuilder;

public class UnityBuild2
{
    public void Run(
        string? projectPath,
        string? unityVersion,
        string? buildTarget,
        string? subTarget,
        string? buildPath
    )
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var log = Path.Combine(home, "ci-cache", "logs", $"build_{buildTarget}.log");

        var args = new UnityArgs
        {
            ExecuteMethod = "BuildSystem.BuildScript.BuildPlayer",
            ProjectPath = projectPath,
            LogPath = log,
            BuildPath = buildPath,
            BuildTarget = buildTarget,
            SubTarget = subTarget,
            CustomArgs = null
        };

        var path = UnityPath.GetDefaultUnityPath(unityVersion);
        var unity = new UnityRunner(path);
        unity.Run(args);

        if (unity.ExitCode == 0)
            return;

        Console.WriteLine($"Unity build failed. Code: {unity.ExitCode}: {unity.Message}");
        Environment.Exit(1);
    }
}
