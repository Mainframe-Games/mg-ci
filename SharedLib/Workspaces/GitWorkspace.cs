namespace SharedLib;

public class GitWorkspace : Workspace
{
    public GitWorkspace(string name, string directory) : base(name, directory)
    {
        
    }

    public override void Clear()
    {
        Cmd.Run("git", "reset --hard");
        RefreshMetaData();
    }

    public override void Update(int changeSetId = -1)
    {
        Cmd.Run("git", "pull");
        RefreshMetaData();
    }

    public override void GetCurrent(out int changesetId, out string guid)
    {
        var currentDir = Environment.CurrentDirectory;
        Environment.CurrentDirectory = Directory;
		
        var cmdRes = Cmd.Run(
            "git", "log --oneline -n 1",
            logOutput: false);
		
        Environment.CurrentDirectory = currentDir;

        var split = cmdRes.output.Split(' ');
        changesetId = 0;
        guid = split[0];
    }

    public override int GetPreviousChangeSetId(string key)
    {
        throw new NotImplementedException();
    }

    public string[] GetChangeLog(string? curSha, string? prevSha, bool print = true)
    {
        // get all commit messages
        var (exitCode, output) = Cmd.Run("git", $"log --pretty=format:\"%s\" {prevSha}..{curSha}", print);
        return output.Split('\n');
    }

    public override void Commit(string commitMessage)
    {
        Cmd.Run("git", "add .");
        Cmd.Run("git", $"commit -m \"{commitMessage}\"");
        Cmd.Run("git", $"push origin {Branch}");
    }

    public override void SwitchBranch(string? branchPath)
    {
        Cmd.Run("git", $"switch {branchPath}");
    }
}