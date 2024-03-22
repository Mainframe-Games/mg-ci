namespace SharedLib;

public class GitWorkspace : Workspace
{
    protected GitWorkspace(string name, string directory) : base(name, directory)
    {
        
    }

    public override void Clear()
    {
        // Cmd.Run("cm", $"unco -a \"{Directory}\"");
        RefreshMetaData();
    }

    public override void Update(int changeSetId = -1)
    {
        // todo: implement
        RefreshMetaData();
    }

    public override void GetCurrent(out int changesetId, out string guid)
    {
        throw new NotImplementedException();
    }

    public override int GetPreviousChangeSetId(string key)
    {
        throw new NotImplementedException();
    }

    public override string[] GetChangeLog(int curId, int prevId, bool print = true)
    {
        throw new NotImplementedException();
    }

    public override void Commit(string commitMessage)
    {
        throw new NotImplementedException();
    }

    public override void SwitchBranch(string? branchPath)
    {
        throw new NotImplementedException();
    }
}