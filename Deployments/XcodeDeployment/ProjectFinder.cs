using Tomlyn;
using Tomlyn.Model;

namespace XcodeDeployment;

internal static class ProjectFinder
{
    public static (DirectoryInfo projDir, TomlTable projToml) GetProjectDirectory(Guid projectGuid)
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

            // Return the parent directory of the project.toml file
            var dir = toml.Directory?.Parent ?? throw new NullReferenceException();
            return (dir, projectToml);
        }

        throw new Exception($"Project not found in cache: {projectGuid}");
    }
}
