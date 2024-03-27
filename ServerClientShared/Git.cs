using LibGit2Sharp;

namespace ServerClientShared;

/// <summary>
/// src: https://github.com/libgit2/libgit2sharp/wiki
/// </summary>
public class Git(string path)
{
    private readonly Repository _repository = new(path);
    private static Signature Author => new("build-bot", "email@build.bot", DateTimeOffset.Now);

    public static string Clone(string url, string cloneToPath, string branch)
    {
        if (Repository.IsValid(cloneToPath))
            return cloneToPath;

        var res = Repository.Clone(
            url,
            cloneToPath,
            new CloneOptions { RecurseSubmodules = true, BranchName = branch }
        );
        Console.WriteLine($"Clone: {res}");
        return res;
    }

    public void Pull()
    {
        Commands.Pull(_repository, Author, new PullOptions());
    }

    public void SwitchBranch(string branchName)
    {
        Commands.Checkout(_repository, _repository.Branches[branchName]);
    }

    public void Commit(string message)
    {
        Commands.Stage(_repository, "*");
        _repository.Commit(message, Author, Author);
    }

    public void Push()
    {
        _repository.Network.Push(_repository.Head);
    }

    public void Dispose()
    {
        _repository.Dispose();
    }

    /// <summary>
    /// Returns the branches from a remote repository without cloning it.
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    public static List<string> GetBranchesFromRemote(string url)
    {
        var refs = Repository.ListRemoteReferences(url);
        var branches = new List<string>();

        foreach (var reference in refs)
        {
            if (!reference.CanonicalName.Contains("refs/heads/"))
                continue;

            Console.WriteLine($"Reference: {reference}");
            var branchName = reference.CanonicalName.Split('/')[^1];
            branches.Add(branchName);
        }

        return branches;
    }
}
