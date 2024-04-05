using System.Net;
using System.Net.WebSockets;
using System.Text;

namespace SocketServer;

public sealed class WebSocketClient(string url)
{
    private ClientWebSocket? _clientWebSocket = new();
    public bool IsAlive => _clientWebSocket?.State is WebSocketState.Open;

    public async Task ConnectAsync()
    {
        do
        {
            try
            {
                _clientWebSocket = new ClientWebSocket();
                await _clientWebSocket.ConnectAsync(new Uri(url), CancellationToken.None);
                await Task.Delay(1000);
            }
            catch (WebSocketException)
            {
                // ignore as we can keep polling for connection
            }
        } while (!IsAlive);

        Console.WriteLine("[Client] Connected to server.");

        ListenForMessages();
    }

    private async void ListenForMessages()
    {
        while (IsAlive)
        {
            var buffer = new byte[Config.BUFFERS_SIZE];
            WebSocketReceiveResult result;

            do
            {
                result = await _clientWebSocket!.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if (!result.EndOfMessage)
                    Array.Resize(ref buffer, buffer.Length * 2);
            } while (!result.EndOfMessage);

            Console.WriteLine(
                $"[Client] Received {result.MessageType} packet size: {Print.ToByteSizeString(result.Count)}");

            switch (result.MessageType)
            {
                case WebSocketMessageType.Text:
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    OnTextMessage(message);
                    break;
                case WebSocketMessageType.Binary:
                    OnBinaryMessage(buffer);
                    break;
                case WebSocketMessageType.Close:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public async Task SendMessage(string message)
    {
        var buffer = Encoding.UTF8.GetBytes(message);
        Console.WriteLine(
            $"[Client] Sending {WebSocketMessageType.Text} packet size: {Print.ToByteSizeString(buffer.Length)}");
        await _clientWebSocket!.SendAsync(buffer, WebSocketMessageType.Text,
            WebSocketMessageFlags.DisableCompression | WebSocketMessageFlags.EndOfMessage,
            CancellationToken.None);
    }

    public async Task SendMessage(byte[] data)
    {
        Console.WriteLine(
            $"[Client] Sending {WebSocketMessageType.Binary} packet size: {Print.ToByteSizeString(data.Length)}");

        await _clientWebSocket!.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Binary,
            WebSocketMessageFlags.DisableCompression | WebSocketMessageFlags.EndOfMessage,
            CancellationToken.None);
    }

    public async Task Send(DirectoryInfo dir)
    {
        var rootPath = dir.FullName;
        var files = dir.GetFiles("*", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            var ms = new MemoryStream();
            await using var writer = new BinaryWriter(ms);

            var fileLocalPath = file
                .FullName.Replace(rootPath, string.Empty)
                .Replace('\\', '/')
                .Trim('/');

            // add dir name
            writer.Write(dir.Name);

            // add fileName
            writer.Write(fileLocalPath);

            // add file data
            var data = await File.ReadAllBytesAsync(file.FullName);
            writer.Write(data.Length);
            writer.Write(data);

            await SendMessageFragmented(ms.ToArray());
        }
    }

    public async Task Send(FileInfo file)
    {
        if (!file.Exists)
            throw new FileNotFoundException();

        var ms = new MemoryStream();
        await using var writer = new BinaryWriter(ms);

        var fileName = Path.GetFileName(file.FullName);

        // add fileName
        writer.Write(fileName);

        // add file data
        var data = await File.ReadAllBytesAsync(file.FullName);
        writer.Write(data.Length);
        writer.Write(data);

        await SendMessageFragmented(ms.ToArray());
    }

    public async Task SendMessageFragmented(byte[] data)
    {
        Console.WriteLine(
            $"[Client] Sending {WebSocketMessageType.Binary} packet size: {Print.ToByteSizeString(data.Length)}");

        var frags = Fragmentation.Fragment(data);
        var packet = new BuildUploadPacket
        {
            TotalBytes = (uint)data.Length,
            FragmentCount = (uint)frags.Count,
        };
        for (int i = 0; i < frags.Count; i++)
        {
            packet.FragmentIndex = (uint)i;
            packet.Fragments = frags[i];

            var bytes = packet.GetBytes();
            await _clientWebSocket!.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Binary,
                WebSocketMessageFlags.DisableCompression | WebSocketMessageFlags.EndOfMessage,
                CancellationToken.None);
        }
    }

    public async Task Close()
    {
        await _clientWebSocket!.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing connection",
            CancellationToken.None);
    }

    private void OnTextMessage(string message)
    {
        Console.WriteLine($"[Client] OnMessage: {message}");
    }

    private void OnBinaryMessage(byte[] data)
    {
    }
}