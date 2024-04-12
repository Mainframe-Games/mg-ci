using System.Text;

namespace SocketServer;

internal enum MessageType : byte
{
    Connection,
    Close,
    String,
    Binary,
    Json
}

internal class TpcPacket
{
    private static ulong NextId;

    /// <summary>
    /// Unique ID for packet
    /// </summary>
    public ulong Id { get; private set; }

    /// <summary>
    /// Empty service name will send to all services
    /// </summary>
    public string ServiceName { get; private set; } = string.Empty;

    public MessageType Type { get; private set; }
    public byte[] Data { get; private set; } = [];

    public TpcPacket()
    {
    }

    public TpcPacket(MessageType type, byte[] data)
    {
        Type = type;
        Data = data;
    }

    public TpcPacket(string serviceName, MessageType type, byte[] data)
    {
        ServiceName = serviceName;
        Type = type;
        Data = data;
    }

    public byte[] GetBytes()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        Id = ++NextId;
        writer.Write(Id); // uint64
        writer.Write(ServiceName); // string
        writer.Write((byte)Type); // byte
        writer.Write(Data.Length); // int32

        if (Data.Length > 0)
            writer.Write(Data); // byte[]

        return ms.ToArray();
    }

    public void Read(byte[] bytes)
    {
        using var ms = new MemoryStream(bytes);
        using var reader = new BinaryReader(ms);

        Id = reader.ReadUInt64(); // uint64
        ServiceName = reader.ReadString(); // string
        Type = (MessageType)reader.ReadByte(); // byte
        var dataLength = reader.ReadInt32(); // int32

        if (dataLength > 0)
            Data = reader.ReadBytes(dataLength); // byte[]
    }

    public override string ToString()
    {
        var str = new StringBuilder();
        str.AppendLine();
        str.AppendLine($"- Id: {Id}");
        str.AppendLine($"  ServiceName: {ServiceName}");
        str.AppendLine($"  Type: {Type}");
        str.AppendLine($"  Data: {Data.Length}");
        return str.ToString();
    }
}