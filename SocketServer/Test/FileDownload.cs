namespace SocketServer.Test;

public static class FileDownload
{
    private static readonly Dictionary<string, Packet> _packets = [];
    public static event Action<string>? OnFileDownloadCompleted;
    public static event Action<string, double>? OnFileDownloadProgress;

    public static void Download(byte[] data)
    {
        using var ms = new MemoryStream(data);
        using var reader = new BinaryReader(ms);
        var dirName = reader.ReadString(); // string
        var filePath = reader.ReadString(); // string
        var totalBytes = reader.ReadUInt32(); // uint32
        var fragmentLength = reader.ReadInt32(); // int32
        var fragment = reader.ReadBytes(fragmentLength); // byte[]

        var key = dirName + filePath;

        if (!_packets.ContainsKey(key))
            _packets.Add(key, CreateNewDownloadPacket(dirName, filePath, (uint)totalBytes));

        var packet = _packets[key];
        packet.Write(fragment);

        OnFileDownloadProgress?.Invoke(packet.Path, packet.Progress);

        if (!packet.IsComplete)
            return;

        Console.WriteLine($"Download complete [{dirName}]");
        OnFileDownloadCompleted?.Invoke(packet.Path);
    }

    private static Packet CreateNewDownloadPacket(string dirName, string inFilePath, uint totalBytes)
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var filePath = Path.Combine(home, ".ci-cache", "Downloads", dirName, inFilePath);
        var fileInfo = new FileInfo(filePath);

        if (fileInfo.Exists)
            fileInfo.Delete();

        if (fileInfo.Directory?.Exists is not true)
            fileInfo.Directory?.Create();

        return new Packet
        {
            Path = filePath,
            Steam = new FileStream(filePath, FileMode.Open),
            TotalBytes = totalBytes
        };
    }
    
    private struct Packet
    {
        public string Path;
        public FileStream Steam;
        public uint TotalBytes;
        private uint _currentBytes;

        public double Progress => _currentBytes / (double)TotalBytes * 100;
        public bool IsComplete { get; private set; }

        public void Write(byte[] data)
        {
            Steam.Write(data, 0, data.Length);
            _currentBytes += (uint)data.Length;
            IsComplete = _currentBytes == TotalBytes;
        }
    }
}