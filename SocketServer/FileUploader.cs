namespace SocketServer;

public static class FileUploader
{
    public static async Task UploadDirectory(DirectoryInfo rootDir, IService service)
    {
        var files = rootDir.GetFiles("*", SearchOption.AllDirectories);
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

                await service.SendBinary(ms.ToArray());
                await Task.Delay(10); // need to delay to give server some time to process
            }
        }
    }
}
