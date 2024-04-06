namespace SocketServer;

public static class FileUploader
{
    private static readonly Queue<(DirectoryInfo dir, IService service)> _uploadQueue = new();
    private static Task? _dispatchTask;

    public static void UploadDirectory(DirectoryInfo rootDir, IService service)
    {
        _uploadQueue.Enqueue((rootDir, service));
        Upload();
    }

    private static async void Upload()
    {
        if (_dispatchTask is not null && !_dispatchTask.IsCompleted)
            return;

        _dispatchTask = UploadDirectoryDispatch();
        await _dispatchTask;
        _dispatchTask = null;
    }

    private static async Task UploadDirectoryDispatch()
    {
        while (_uploadQueue.Count > 0)
        {
            try
            {
                var (rootDir, service) = _uploadQueue.Dequeue();
                var files = rootDir.GetFiles("*", SearchOption.AllDirectories);
                Console.WriteLine($"Upload started: {rootDir.FullName}");
                foreach (var file in files)
                {
                    var data = await File.ReadAllBytesAsync(file.FullName); // TODO; could open file stream instead
                    var fileFrags = Fragmentation.Fragment(data);

                    var fileLocalPath = file.FullName.Replace(rootDir.FullName, string.Empty)
                        .Replace('\\', '/')
                        .Trim('/');

                    foreach (var frag in fileFrags)
                    {
                        var ms = new MemoryStream();
                        await using var writer = new BinaryWriter(ms);

                        // add dir name
                        writer.Write(rootDir.Name); // string
                        writer.Write(fileLocalPath); // string
                        writer.Write((uint)file.Length); // uint32
                        writer.Write(frag.Length); // int32
                        writer.Write(frag); // byte[]

                        service.SendBinary(ms.ToArray());
                    }
                }

                Console.WriteLine($"Upload complete: {rootDir.FullName}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"ERROR: {e}");
                Environment.Exit(1);
            }
        }
    }
}
