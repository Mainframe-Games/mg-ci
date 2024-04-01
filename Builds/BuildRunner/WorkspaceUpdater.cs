using Tomlyn;
using Tomlyn.Model;

namespace BuildRunner;

public static class WorkspaceUpdater
{
    public static RunnerWorkspace? PrepareWorkspace(string projectId)
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

            if (projectToml["guid"].ToString() != projectId)
                continue;

            var gitUrl = GetValue<string>(projectToml, "settings", "git_repository_url");
            var branch = GetValue<string>(projectToml, "settings", "branch");

            var projectRoot =
                toml.Directory?.Parent?.FullName ?? throw new NullReferenceException();
            var workspace = new RunnerWorkspace(projectRoot) { GitUrl = gitUrl, Branch = branch };
            workspace.Update();
            return workspace;
        }

        return null;
    }

    private static T? GetValue<T>(TomlTable table, params string[] keys)
    {
        var root = table;

        foreach (var key in keys)
        {
            if (!root.TryGetValue(key, out var value))
                continue;

            if (value is TomlTable subTable)
                root = subTable;
            else
                return (T)value;
        }

        return default;
    }
}
