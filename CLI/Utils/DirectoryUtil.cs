namespace CLI.Utils;

public static class DirectoryUtil
{
    public static void DeleteDirectoryExists(string path, bool recreate)
    {
        if (Directory.Exists(path))
            Directory.Delete(path, true);
        
        if (recreate)
            CreateDirectoryIfNotExists(path);
    }

    public static void CreateDirectoryIfNotExists(string path)
    {
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
    }
}