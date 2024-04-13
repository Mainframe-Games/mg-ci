namespace SocketServer.Utils;

public static class FileCopier
{
    public static void Copy(Guid projectId, DirectoryInfo sourceDir)
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var destinationPath = Path.Combine(
            home,
            "ci-cache",
            "Downloads",
            projectId.ToString(),
            sourceDir.Name
        );

        Console.WriteLine("Copying...");
        Console.WriteLine($" From: {sourceDir.FullName}");
        Console.WriteLine($" To: {destinationPath}");
        CopyDirectory(sourceDir.FullName, destinationPath);
        Console.WriteLine("Copy Complete");
    }

    private static void CopyDirectory(string sourceDirName, string destDirName)
    {
        // Get the subdirectories for the specified directory.
        var dir = new DirectoryInfo(sourceDirName);

        if (!dir.Exists)
        {
            throw new DirectoryNotFoundException(
                "Source directory does not exist or could not be found: " + sourceDirName
            );
        }

        var dirs = dir.GetDirectories();

        // If the destination directory doesn't exist, create it.
        Directory.CreateDirectory(destDirName);

        // Get the files in the directory and copy them to the new location.
        var files = dir.GetFiles();
        foreach (var file in files)
        {
            var tempPath = Path.Combine(destDirName, file.Name);
            file.CopyTo(tempPath, false);
        }

        // If copying subdirectories, copy them and their contents to new location.
        foreach (var subdir in dirs)
        {
            var tempPath = Path.Combine(destDirName, subdir.Name);
            CopyDirectory(subdir.FullName, tempPath);
        }
    }
}
