using LibGit2Sharp;
using LibGit2Sharp.Handlers;

namespace SharedLib;

public class GitWorkspace : Workspace
{
    private readonly Repository _repository;
    private readonly Signature _author = new("build-bot", "email@build.bot", DateTimeOffset.Now);

    public GitWorkspace(Repository repository, string name, string directory, string projectId)
        : base(name, directory, projectId)
    {
        _repository = repository;
    }

    public override void Clear()
    {
        _repository.Reset(ResetMode.Hard, _repository.Head.Tip);
        RefreshMetaData();
    }

    public override void Update(int changeSetId = -1)
    {
        Commands.Pull(_repository, _author, new PullOptions());
        RefreshMetaData();
    }

    public override void GetCurrent(out int changesetId, out string guid)
    {
        changesetId = 0;

        // get repo log
        var commits = _repository.Commits.QueryBy(new CommitFilter { FirstParentOnly = true });
        guid = commits?.First().Sha ?? string.Empty;
    }

    public override int GetPreviousChangeSetId(string key)
    {
        throw new NotImplementedException();
    }

    public string[] GetChangeLog(string? curSha, string? prevSha)
    {
        var commits = _repository.Commits.QueryBy(new CommitFilter { FirstParentOnly = true });

        var logs = new List<string>();

        foreach (var commit in commits)
        {
            if (commit.Sha == prevSha)
                break;

            logs.Add(commit.Message);
        }

        return logs.ToArray();
    }

    public override void Commit(string commitMessage)
    {
        try
        {
            Commands.Stage(_repository, "*");
            _repository.Commit(commitMessage, _author, _author);
            _repository.Network.Push(
                _repository.Head,
                new PushOptions { CredentialsProvider = CredentialsHandler }
            );
        }
        catch (EmptyCommitException e)
        {
            Console.WriteLine(e);
        }
    }

    private static Credentials CredentialsHandler(
        string url,
        string usernamefromurl,
        SupportedCredentialTypes types
    )
    {
        return new DefaultCredentials();
    }

    public override void SwitchBranch(string? branchPath)
    {
        if (_repository.Head.FriendlyName == branchPath)
            return;

        Commands.Checkout(_repository, _repository.Branches[branchPath]);
    }
}
