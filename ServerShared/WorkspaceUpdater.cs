using Tomlyn;
using Tomlyn.Model;

namespace ServerShared;

public static class WorkspaceUpdater
{
    public static Workspace PrepareWorkspace(Guid projectGuid, string branch)
    {
        var (projDir, projToml) = GetProjectDirectory(projectGuid);
        
        var gitUrl = projToml.GetValue<string>("settings", "git_repository_url");
        
        var workspace = new Workspace(projDir.FullName) { GitUrl = gitUrl, Branch = branch };
        workspace.Update();
        return workspace;
    }
    
    private static (DirectoryInfo projDir, TomlTable projToml) GetProjectDirectory(Guid projectGuid)
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
