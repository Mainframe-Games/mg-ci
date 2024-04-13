using System.Net.Sockets;
using System.Text;

namespace SocketServer;

public interface INetworkDispatcher
{
    string Alias { get; }
    NetworkStream NetworkStream { get; }
}

internal class PacketDispatcher(INetworkDispatcher dispatcher)
{
    private readonly Queue<TpcPacket> _sendQueue = new();
    private Task? _sendTask;

    internal void Send(TpcPacket packet)
    {
        _sendQueue.Enqueue(packet);
        _sendTask ??= Task.Run(SendInternal);
    }

    internal async Task SendAsync(TpcPacket packet)
    {
        var data = packet.GetBytes();
        // var checksum = CheckSum.Build(data);

        // Console.WriteLine($"[{dispatcher.Alias}] Send packet {packet}");
        // Console.WriteLine();
        // Console.WriteLine($"[{dispatcher.Alias}] Sending");
        // Console.WriteLine($"  Checksum: {checksum}");
        // Console.WriteLine($"  Size: {Print.ToByteSizeString(data.Length)}");
        // Console.WriteLine();

        // await dispatcher.NetworkStream.WriteAsync(Encoding.UTF8.GetBytes(checksum));
        await dispatcher.NetworkStream.WriteAsync(BitConverter.GetBytes(data.Length));
        await dispatcher.NetworkStream.WriteAsync(data);
        await Task.Delay(10); // delay to prevent spamming
    }

    private async Task SendInternal()
    {
        while (_sendQueue.Count > 0)
        {
            var packet = _sendQueue.Dequeue();
            await SendAsync(packet);
        }

        // Console.WriteLine($"[{dispatcher.Alias}] Send queue empty");
        _sendTask = null;
    }
}
