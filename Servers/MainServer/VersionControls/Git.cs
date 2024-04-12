using System.Diagnostics;
using System.Text;
using LibGit2Sharp;

namespace MainServer.VersionControls;

internal class Git(
    string projectPath,
    string url,
    string branch,
    string username,
    string accessToken
)
{
    private readonly Signature _author = new("build-bot", "email@build.bot", DateTimeOffset.Now);
    private readonly UsernamePasswordCredentials _usernamePasswordCredentials =
        new() { Username = username, Password = accessToken };

    public void Update()
    {
        if (!Repository.IsValid(projectPath))
        {
            Repository.Clone(
                url,
                projectPath,
                new CloneOptions
                {
                    RecurseSubmodules = true,
                    BranchName = branch,
                    FetchOptions = { CredentialsProvider = CredentialsHandler },
                    OnCheckoutProgress = CheckoutProgress
                }
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
                    CredentialsProvider = CredentialsHandler,
                    TagFetchMode = TagFetchMode.All
                },
            }
        );

        RunProcess("lfs pull");
    }

    private static void CheckoutProgress(string path, int completedsteps, int totalsteps)
    {
        Console.WriteLine(
            $"Git Checkout ({completedsteps}/{totalsteps} {completedsteps / (double)totalsteps * 100:0}%): {path}"
        );
    }

    private void Chmod(string pathToFile)
    {
        // no need to chmod on windows
        if (OperatingSystem.IsWindows())
            return;

        var info = new ProcessStartInfo
        {
            FileName = "chmod",
            Arguments = $"+x {pathToFile}",
            WorkingDirectory = projectPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        Process.Start(info)!.WaitForExit();
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

        const string GIT_ASK = "git-askpass.sh";

        if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(accessToken))
        {
            var sh = new StringBuilder();
            sh.AppendLine("#!/bin/bash");
            sh.AppendLine("echo \"$GIT_PASSWORD\"");
            info.EnvironmentVariables["GIT_USERNAME"] = username;
            info.EnvironmentVariables["GIT_PASSWORD"] = accessToken;
            File.WriteAllText(GIT_ASK, sh.ToString());

            var gitAsk = new FileInfo(GIT_ASK);
            info.EnvironmentVariables["GIT_ASKPASS"] = gitAsk.FullName;
            Chmod(gitAsk.FullName);
        }

        Console.WriteLine($"Running: git {args}");
        var process = Process.Start(info) ?? throw new NullReferenceException();

        process.OutputDataReceived += (sender, eventArgs) =>
        {
            if (!string.IsNullOrEmpty(eventArgs.Data))
                Console.WriteLine($"Git {args}: {eventArgs.Data}");
        };
        process.BeginOutputReadLine();

        process.ErrorDataReceived += (sender, eventArgs) =>
        {
            if (!string.IsNullOrEmpty(eventArgs.Data))
                Console.WriteLine($"[ERROR] Git {args}: {eventArgs.Data}");
        };
        process.BeginErrorReadLine();

        process.WaitForExit();

        if (File.Exists(GIT_ASK))
            File.Delete(GIT_ASK);
    }

    public string[] GetChangeLog()
    {
        using var repo = new Repository(projectPath);
        var tag = repo.Tags.LastOrDefault(x => x.FriendlyName.StartsWith('v'));
        var commits = repo.Commits.QueryBy(
            new CommitFilter { FirstParentOnly = true, ExcludeReachableFrom = tag }
        );
        var logs = new List<string>();

        foreach (var commit in commits)
        {
            var message = commit.Message?.Trim();

            if (string.IsNullOrEmpty(message))
                continue;

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

    public void Commit(string commitMessage, IEnumerable<string> filesToCommit)
    {
        using var repository = new Repository(projectPath);
        Commands.Stage(repository, filesToCommit);
        repository.Commit(commitMessage, _author, _author);
        repository.Network.Push(
            repository.Head,
            new PushOptions { CredentialsProvider = CredentialsHandler }
        );
    }

    public void Tag(string inTag)
    {
        using var repository = new Repository(projectPath);
        repository.ApplyTag(inTag, _author, inTag);
        repository.Network.Push(
            repository.Head,
            new PushOptions { CredentialsProvider = CredentialsHandler }
        );
        RunProcess("push --tags");
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
