namespace SharedLib;

public class PlasticWorkspace : Workspace
{
    private PlasticWorkspace(string name, string directory) : base(name, directory)
    {
        
    }
    
    public static bool TryAskWorkspace(out Workspace workspace)
    {
        var (exitCode, output) = Cmd.Run("cm", "workspace", logOutput: false);

        if (exitCode != 0)
            throw new Exception(output);

        var workspaces = GetAvailableWorkspaces();
        var workspaceNames = workspaces.Select(x => x.Name).ToList();

        if (!Cmd.Choose("Choose workspace", workspaceNames, out var index))
        {
            workspace = null!;
            return false;
        }
		
        workspace = workspaces[index];
        Logger.Log($"Chosen workspace: {workspace}");
        return true;
    }
    
    public static List<Workspace> GetAvailableWorkspaces()
    {
        var (exitCode, output) = Cmd.Run("cm", "workspace", logOutput: false);

        if (exitCode != 0)
            throw new Exception(output);

        var workSpacesArray = output.Split(Environment.NewLine);
        var workspaces = new List<Workspace>();

        foreach (var workspace in workSpacesArray)
        {
            try
            {
                var split = workspace.Split('@');
                var name = split[0];
                var path = split[1].Replace(Environment.MachineName, string.Empty).Trim();
                var ws = new PlasticWorkspace(name, path);
                workspaces.Add(ws);
            }
            catch (Exception e)
            {
                Logger.Log($"Error with workspace: {workspace}");
                Logger.Log(e);
            }
        }

        return workspaces;
    }
    
    public static Workspace? GetWorkspaceFromName(string? workspaceName)
    {
        var workspaces = GetAvailableWorkspaces();
        return workspaces.FirstOrDefault(x => string.Equals(x.Name, workspaceName, StringComparison.OrdinalIgnoreCase));
    }

    public override void Clear()
    {
        Cmd.Run("cm", $"unco -a \"{Directory}\"");
        RefreshMetaData();
    }
    
    public override void Update(int changeSetId = -1)
    {
        // get all the latest updates
        Cmd.Run("cm", $"update \"{Directory}\"");
		
        // set to a specific change set
        if (changeSetId > 0)
        {
            var (exitCode, output) = Cmd.Run("cm", $"switch cs:{changeSetId} --workspace=\"{Directory}\"");
			
            if (exitCode != 0 || output.ToLower().Contains("does not exist"))
                throw new Exception($"Plastic update error: {output}");
        }

        RefreshMetaData();
    }

    public override void GetCurrent(out int changesetId, out string guid)
    {
        var currentDir = Environment.CurrentDirectory;
        Environment.CurrentDirectory = Directory;
		
        var cmdRes = Cmd.Run(
            "cm", $"find changeset \"where branch='{Branch}'\" \"order by date desc\" \"limit 1\" --format=\"{{changesetid}} {{guid}}\" --nototal",
            logOutput: false);
		
        Environment.CurrentDirectory = currentDir;

        var split = cmdRes.output.Split(' ');
        changesetId = int.TryParse(split[0], out var id) ? id : 0;
        guid = split[1];
    }

    public override int GetPreviousChangeSetId(string key)
    {
        var req = Cmd.Run("cm",
            $"find changeset \"where branch='{Branch}' and comment like '%{key}%'\" \"order by date desc\" \"limit 1\" --format=\"{{changesetid}}\" --nototal",
            logOutput: false);
		
        // if empty from branch, try again in 'main'
        if (string.IsNullOrEmpty(req.output))
            req = Cmd.Run("cm", 
                $"find changeset \"where branch='main' and comment like '%{key}%'\" \"order by date desc\" \"limit 1\" --format=\"{{changesetid}}\" --nototal",
                logOutput: false);
		
        return int.TryParse(req.output, out var cs) ? cs : 0;
    }

    /// <summary>
    /// Gets all change logs between two changeSetIds
    /// </summary>
    public string[] GetChangeLog(int curId, int prevId, bool print = true)
    {
        var dirBefore = Environment.CurrentDirectory;
        Environment.CurrentDirectory = Directory;
		
        var raw = Cmd.Run("cm", $"log --from=cs:{prevId} cs:{curId} --csformat=\"{{comment}}\"").output;
        var changeLog = raw.Split(Environment.NewLine).Reverse().ToList();
        changeLog.RemoveAll(x => x.StartsWith("_")); // remove all ignores
		
        if (print)
            Logger.Log($"___Change Logs___\n{string.Join("\n", changeLog)}");
		
        Environment.CurrentDirectory = dirBefore;
        return changeLog.ToArray();
    }

    public override void Commit(string commitMessage)
    {
        // update in case there are new changes in coming otherwise it will fail
        // TODO: need to find a way to automatically resolve conflicts with cloud
        Update();

        var status = Cmd.Run("cm", "status --short").output;
        var files = status.Split(Environment.NewLine);
        var filesToCommit = files
            .Where(x => x.Contains(".vdf")
                        || x.Contains(PROJ_SETTINGS_ASSET)
                        || x.Contains(APP_VERSION_TXT)
                        || x.Contains(WORKSPACE_META_JSON))
            .ToList();

        // commit changes
        var filesStr = $"\"{string.Join("\" \"", filesToCommit)}\"";
        Logger.Log($"Commit: \"{commitMessage}\"");
        Cmd.Run("cm", $"ci {filesStr} -c=\"{commitMessage}\"");
    }

    public override void SwitchBranch(string? branchPath)
    {
        var res = Cmd.Run("cm", $"switch {branchPath} --workspace=\"{Directory}\"");
        if (res.exitCode != 0)
            throw new Exception(res.output);
        Branch = branchPath;
    }
}