using System.Diagnostics;
using LibGit2Sharp;

namespace ServerShared.VersionControls;

public class Git(string projectPath, string url, string branch, string username, string accessToken)
{
    private readonly Signature _author = new("build-bot", "email@build.bot", DateTimeOffset.Now);
    private readonly UsernamePasswordCredentials _usernamePasswordCredentials = new()
    {
        Username = username,
        Password = accessToken
    };

    public void Update()
    {
        if (!Repository.IsValid(projectPath))
        {
            Repository.Clone(
                url,
                projectPath,
                new CloneOptions { RecurseSubmodules = true, BranchName = branch }
            );

            // init LFS
            RunProcess("lfs install");
        }

        // clear
        using var repo = new Repository(projectPath);
        repo.Reset(ResetMode.Hard, repo.Head.Tip);

        // switch branch
        RunProcess($"switch {branch}");

        // pull
        Commands.Pull(
            repo,
            _author,
            new PullOptions
            {
                FetchOptions = new FetchOptions
                {
                    CredentialsProvider = CredentialsHandler
                }
            }
        );
        
        RunProcess("lfs pull");
    }

    private void RunProcess(string args)
    {
        var info = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = args,
            WorkingDirectory = projectPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        Process.Start(info)!.WaitForExit();
    }

    public string[] GetChangeLog()
    {
        using var repo = new Repository(projectPath);
        var commits = repo.Commits.QueryBy(new CommitFilter { FirstParentOnly = true });
        var logs = new List<string>();

        foreach (var commit in commits)
        {
            var message = commit.Message;

            if (message.Contains("_Build Successful."))
                break;

            if (message.StartsWith('_'))
                continue;

            logs.Add(message);
        }

        return logs.ToArray();
    }
    
    public string GetLatestCommitHash()
    {
        using var repo = new Repository(projectPath);
        return repo.Head.Tip.Sha;
    }
    
    public void Commit(string commitMessage, string[] filesToCommit)
    {
        using var repository = new Repository(projectPath);
        Commands.Stage(repository, ".");
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
            SupportedCredentialTypes.UsernamePassword => _usernamePasswordCredentials,
            SupportedCredentialTypes.Default => new DefaultCredentials(),
            _ => throw new ArgumentOutOfRangeException(nameof(types), types, null)
        };
    }
}