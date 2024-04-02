namespace OffloadServer.Utils;

internal static class WorkspaceUpdater
{
    public static Workspace? PrepareWorkspace(string projectGuid)
    {
        var (projDir, projToml) = ProjectFinder.GetProjectDirectory(projectGuid);
        
        var gitUrl = projToml.GetValue<string>("settings", "git_repository_url");
        var branch = projToml.GetValue<string>("settings", "branch");
        
        var workspace = new Workspace(projDir.FullName) { GitUrl = gitUrl, Branch = branch };
        workspace.Update();
        return workspace;
    }
}
