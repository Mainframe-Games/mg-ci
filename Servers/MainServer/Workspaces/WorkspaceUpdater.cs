using MainServer.Configs;
using MainServer.Workspaces;
using Tomlyn;
using Tomlyn.Model;

namespace ServerShared;

internal static class WorkspaceUpdater
{
    public static Workspace PrepareWorkspace(
        Guid projectGuid,
        string gitUrl,
        string branch,
        ServerConfig serverConfig
    )
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var cacheRoot = new DirectoryInfo(Path.Combine(home, "ci-cache"));
        var projectPath = Path.Combine(cacheRoot.FullName, projectGuid.ToString());
        var workspace = new Workspace(projectPath, serverConfig)
        {
            GitUrl = gitUrl,
            Branch = branch
        };

        workspace.Update();
        return workspace;
    }

    private static bool TryGetProjectDirectory(
        Guid projectGuid,
        out DirectoryInfo? projDir,
        out TomlTable? projToml
    )
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
            projDir = dir;
            projToml = projectToml;
            return true;
        }

        projDir = null;
        projToml = null;
        return false;
    }
}
