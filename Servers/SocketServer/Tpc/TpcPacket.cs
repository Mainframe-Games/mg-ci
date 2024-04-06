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
    /// <summary>
    /// Unique ID for packet
    /// </summary>
    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>
    /// Empty service name will send to all services
    /// </summary>
    public string ServiceName { get; private set; } = string.Empty;
    public MessageType Type { get; private set; }
    public byte[] Data { get; private set; } = Array.Empty<byte>();

    public TpcPacket() { }

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
        var ms = new MemoryStream();
        var writer = new BinaryWriter(ms);
        {
            writer.Write(Id.ToString()); // string
            writer.Write(ServiceName); // string
            writer.Write((byte)Type); // byte
            writer.Write(Data.Length); // int32

            if (Data.Length > 0)
                writer.Write(Data); // byte[]
        }
        var data = ms.ToArray();

        writer.Dispose();
        ms.Dispose();

        return data;
    }

    public void Read(byte[] bytes)
    {
        var ms = new MemoryStream(bytes);
        var reader = new BinaryReader(ms);
        {
            Id = Guid.Parse(reader.ReadString()); // string
            ServiceName = reader.ReadString(); // string
            Type = (MessageType)reader.ReadByte(); // byte
            var dataLength = reader.ReadInt32(); // int32

            if (dataLength > 0)
                Data = reader.ReadBytes(dataLength); // byte[]
        }

        reader.Dispose();
        ms.Dispose();
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
