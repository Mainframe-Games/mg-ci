using LibGit2Sharp;

namespace SharedLib;

public class GitWorkspace(Repository repository, string name, string directory, string projectId)
    : Workspace(name, directory, projectId)
{
    private readonly Signature _author = new("build-bot", "email@build.bot", DateTimeOffset.Now);
    private readonly UsernamePasswordCredentials _userCredentials = new();

    public void SetCredentials(string? username, string? password)
    {
        _userCredentials.Username = username;
        _userCredentials.Password = password;
    }

    public override void Clear()
    {
        repository.Reset(ResetMode.Hard, repository.Head.Tip);
        RefreshMetaData();
    }

    public override void Update(int changeSetId = -1)
    {
        Commands.Pull(
            repository,
            _author,
            new PullOptions
            {
                FetchOptions = new FetchOptions { CredentialsProvider = CredentialsHandler }
            }
        );

        // lfs
        var env = Environment.CurrentDirectory;
        Environment.CurrentDirectory = Directory;
        Cmd.Run("git", $"lfs pull");
        Environment.CurrentDirectory = env;

        RefreshMetaData();
    }

    public override void GetCurrent(out int changesetId, out string guid)
    {
        changesetId = 0;

        // get repo log
        var commits = repository.Commits.QueryBy(new CommitFilter { FirstParentOnly = true });
        guid = commits?.First().Sha ?? string.Empty;
    }

    public override int GetPreviousChangeSetId(string key)
    {
        throw new NotImplementedException();
    }

    public string[] GetChangeLog(string? curSha, string? prevSha)
    {
        var commits = repository.Commits.QueryBy(new CommitFilter { FirstParentOnly = true });

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
        Commands.Stage(repository, ".");
        repository.Commit(commitMessage, _author, _author);
        repository.Network.Push(
            repository.Head,
            new PushOptions { CredentialsProvider = CredentialsHandler }
        );
    }

    public void Commit(string commitMessage, IEnumerable<string> files)
    {
        foreach (var file in files)
            Commands.Stage(repository, file);

        repository.Commit(commitMessage, _author, _author);
        repository.Network.Push(
            repository.Head,
            new PushOptions { CredentialsProvider = CredentialsHandler }
        );
    }

    private Credentials CredentialsHandler(
        string url,
        string usernamefromurl,
        SupportedCredentialTypes types
    )
    {
        return types switch
        {
            SupportedCredentialTypes.UsernamePassword => _userCredentials,
            SupportedCredentialTypes.Default => new DefaultCredentials(),
            _ => throw new ArgumentOutOfRangeException(nameof(types), types, null)
        };
    }

    public override void SwitchBranch(string? branchPath)
    {
        if (repository.Head.FriendlyName == branchPath)
        {
            Console.WriteLine($"Already on branch '{branchPath}'");
            return;
        }

        Commands.Checkout(repository, repository.Branches[branchPath]);
    }
}
