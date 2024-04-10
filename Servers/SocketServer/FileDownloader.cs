namespace SocketServer;

public static class FileDownloader
{
    private static readonly Dictionary<string, Packet> _packets = [];

    public static void Download(byte[] data)
    {
        using var ms = new MemoryStream(data);
        using var reader = new BinaryReader(ms);
        var projectGuid = new Guid(reader.ReadString()); // string
        var dirName = reader.ReadString(); // string
        var filePath = reader.ReadString(); // string
        var totalBytes = reader.ReadUInt32(); // uint32
        var fragmentLength = reader.ReadInt32(); // int32
        var fragment = reader.ReadBytes(fragmentLength); // byte[]

        var key = Path.Combine(dirName, filePath);

        if (!_packets.ContainsKey(key))
            _packets.Add(key, CreateNewDownloadPacket(projectGuid, dirName, filePath, totalBytes));

        var packet = _packets[key];
        packet.Write(fragment);

        // OnFileDownloadProgress?.Invoke(packet.Path, packet.Progress);
        // packet.PrintProgress();

        if (!packet.IsComplete)
            return;

        // OnFileDownloadCompleted?.Invoke(packet.Path);
        Console.WriteLine($"Download complete [{packet.Path}]");
        _packets.Remove(key);
    }

    private static Packet CreateNewDownloadPacket(
        Guid projectId,
        string dirName,
        string inFilePath,
        uint totalBytes
    )
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var filePath = Path.Combine(
            home,
            "ci-cache",
            "Downloads",
            projectId.ToString(),
            dirName,
            inFilePath
        );
        var fileInfo = new FileInfo(filePath);

        if (fileInfo.Exists)
            fileInfo.Delete();

        if (fileInfo.Directory?.Exists is not true)
            fileInfo.Directory?.Create();

        var fileStream = new FileStream(filePath, FileMode.OpenOrCreate);

        return new Packet
        {
            Path = filePath,
            FileSteam = fileStream,
            TotalBytes = totalBytes
        };
    }

    private struct Packet
    {
        public string Path;
        public FileStream FileSteam;
        public uint TotalBytes;
        private uint CurrentBytes => (uint)FileSteam.Length;

        public double Progress => CurrentBytes / (double)TotalBytes * 100;
        public bool IsComplete { get; private set; }

        public void Write(byte[] data)
        {
            FileSteam.Write(data, 0, data.Length);
            IsComplete = CurrentBytes == TotalBytes;
        }

        public void PrintProgress()
        {
            Console.WriteLine(
                $"Downloading ({Progress:0.00}% | {CurrentBytes}/{TotalBytes}) {Path}..."
            );
        }
    }
}
