using MainServer.Configs;
using MainServer.Workspaces;

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
}
