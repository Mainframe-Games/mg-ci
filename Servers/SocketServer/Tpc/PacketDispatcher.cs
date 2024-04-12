using System.Net.Sockets;

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

    private async Task SendInternal()
    {
        while (_sendQueue.Count > 0)
        {
            var packet = _sendQueue.Dequeue();
            var data = packet.GetBytes();
            Console.WriteLine($"[{dispatcher.Alias}] Send packet {packet}");
            dispatcher.NetworkStream.Write(BitConverter.GetBytes(data.Length));
            dispatcher.NetworkStream.Write(data);
            if (dispatcher.NetworkStream.Length != data.Length)
                throw new Exception("stream length does not match data length");
            dispatcher.NetworkStream.Flush();

            await Task.Delay(10); // delay to prevent spamming
        }

        Console.WriteLine($"[{dispatcher.Alias}] Send queue empty");
        _sendTask = null;
    }
}