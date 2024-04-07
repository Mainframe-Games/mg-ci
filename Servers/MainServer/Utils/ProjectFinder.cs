using Tomlyn;
using Tomlyn.Model;

namespace MainServer.Utils;

internal static class ProjectFinder
{
    public static TomlTable FindProjectPath(Guid projectGuid)
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var cacheRoot = new DirectoryInfo(Path.Combine(home, "ci-cache"));
        var tomls = cacheRoot.GetFiles("*.toml", SearchOption.AllDirectories);

        foreach (var toml in tomls)
        {
            if (toml.Name != "project.toml")
                continue;

            var contents = File.ReadAllText(toml.FullName);
            var projectToml = Toml.ToModel(contents);

            var guid = new Guid(projectToml["guid"].ToString()!);
            if (guid != projectGuid)
                continue;

            return projectToml;
        }

        throw new Exception($"Project not found in cache: {projectGuid}");
    }
}
