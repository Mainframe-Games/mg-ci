namespace SocketServer;

public static class FileUploader
{
    private class UploadQueueData
    {
        public Guid ProjectGuid { get; set; }
        public DirectoryInfo? Directory { get; set; }
        public IService? Service { get; set; }
    }

    private static readonly Queue<UploadQueueData> _uploadQueue = new();
    private static Task? _dispatchTask;

    public static void UploadDirectory(Guid projectGuid, DirectoryInfo rootDir, IService service)
    {
        var data = new UploadQueueData
        {
            ProjectGuid = projectGuid,
            Directory = rootDir,
            Service = service
        };
        _uploadQueue.Enqueue(data);
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
                var queueData = _uploadQueue.Dequeue();
                var projectGuid = queueData.ProjectGuid;
                var rootDir = queueData.Directory ?? throw new NullReferenceException();
                var service = queueData.Service ?? throw new NullReferenceException();

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
                        // Console.WriteLine($"File Uploading: ({Print.ToByteSizeString((int)file.Length)}) {file.FullName}");
                        // Console.WriteLine($"  Frag: ({Print.ToByteSizeString(frag.Length)}) {fileFrags.IndexOf(frag) + 1}/{fileFrags.Count}");

                        var ms = new MemoryStream();
                        await using var writer = new BinaryWriter(ms);

                        // add dir name
                        writer.Write(projectGuid.ToString()); // string
                        writer.Write(rootDir.Name); // string
                        writer.Write(fileLocalPath); // string
                        writer.Write((uint)file.Length); // uint32
                        writer.Write(frag.Length); // int32
                        writer.Write(frag); // byte[]

                        await service.SendBinary(ms.ToArray());
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
