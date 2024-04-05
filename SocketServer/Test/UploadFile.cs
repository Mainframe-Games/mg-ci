namespace SocketServer.Test;

public static class UploadFile
{
    public static async Task Upload(DirectoryInfo rootDir, WebSocketClient client)
    {
        var files = rootDir.GetFiles("*", SearchOption.AllDirectories);
        var totalBytes = (uint)files.Sum(x => x.Length);
        foreach (var file in files)
        {
            Console.WriteLine($"Uploading file: {file.FullName} ({Print.ToByteSizeString((int)file.Length)})");
            
            // add file data
            var data = await File.ReadAllBytesAsync(file.FullName);
            var fileFrags = Fragmentation.Fragment(data);

            var fileLocalPath = file
                .FullName.Replace(rootDir.FullName, string.Empty)
                .Replace('\\', '/')
                .Trim('/');

            foreach (var frag in fileFrags)
            {
                var ms = new MemoryStream();
                await using var writer = new BinaryWriter(ms);

                // add dir name
                writer.Write(rootDir.Name); // string
                writer.Write(fileLocalPath); // string
                writer.Write(totalBytes); // uint32
                writer.Write(frag.Length); // int32
                writer.Write(frag); // byte[]
                
                await client.SendMessage(ms.ToArray());
            }
        }
    }
}