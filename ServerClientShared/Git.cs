using LibGit2Sharp;
using SharedLib;

namespace ServerClientShared;

/// <summary>
/// src: https://github.com/libgit2/libgit2sharp/wiki
/// </summary>
public static class Git
{
    public static string Clone(string url, string cloneToPath, string branch)
    {
        if (Repository.IsValid(cloneToPath))
            return cloneToPath;

        var res = Repository.Clone(
            url,
            cloneToPath,
            new CloneOptions { RecurseSubmodules = true, BranchName = branch }
        );

        // init LFS
        var env = Environment.CurrentDirectory;
        Environment.CurrentDirectory = cloneToPath;
        Cmd.Run("git", "lfs install");
        Environment.CurrentDirectory = env;

        Console.WriteLine($"Clone: {res}");
        return res;
    }

    /// <summary>
    /// Returns the branches from a remote repository without cloning it.
    /// </summary>
    /// <param name="url"></param>
    /// <param name="credentials"></param>
    /// <returns></returns>
    public static List<string> GetBranchesFromRemote(string url, Credentials? credentials = null)
    {
        var branches = new List<string>();

        try
        {
            var refs = Repository.ListRemoteReferences(
                url,
                (inUrl, usernameFromUrl, types) =>
                {
                    return types switch
                    {
                        SupportedCredentialTypes.UsernamePassword => credentials,
                        SupportedCredentialTypes.Default => new DefaultCredentials(),
                        _ => throw new ArgumentOutOfRangeException(nameof(types), types, null)
                    };
                }
            );

            foreach (var reference in refs)
            {
                if (!reference.CanonicalName.Contains("refs/heads/"))
                    continue;

                Console.WriteLine($"Reference: {reference}");
                var branchName = reference.CanonicalName.Split('/')[^1];
                branches.Add(branchName);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        return branches;
    }
}
