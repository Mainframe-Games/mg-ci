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
    public MessageType Type { get; private set; }
    public byte[] Data { get; private set; } = Array.Empty<byte>();

    public TpcPacket() { }

    public TpcPacket(MessageType type, byte[] data)
    {
        Type = type;
        Data = data;
    }

    public byte[] GetBytes()
    {
        var ms = new MemoryStream();
        var writer = new BinaryWriter(ms);
        {
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
            Type = (MessageType)reader.ReadByte(); // byte
            var dataLength = reader.ReadInt32(); // int32

            if (dataLength > 0)
                Data = reader.ReadBytes(dataLength); // byte[]
        }

        reader.Dispose();
        ms.Dispose();
    }
}
