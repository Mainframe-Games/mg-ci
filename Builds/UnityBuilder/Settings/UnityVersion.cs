namespace UnityBuilder.Settings;

internal static class UnityVersion
{
    public static string Get(string projectPath)
    {
        var projectSettings = Path.Combine(projectPath, "ProjectSettings", "ProjectVersion.txt");
        var version = File.ReadAllLines(projectSettings);

        if (version.Length == 0)
            throw new Exception("ProjectVersion.txt is empty");

        return version[0].Split(':')[^1].Trim();
    }
}
