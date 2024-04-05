namespace SocketServer;

internal class TpcPacket
{
    public bool IsClose { get; set; }
    public MessageType Type { get; set; }
    public byte[] Data { get; set; } = Array.Empty<byte>();

    public byte[] GetBytes()
    {
        var ms = new MemoryStream();
        var writer = new BinaryWriter(ms);
        writer.Write((byte)Type);
        writer.Write(IsClose);
        writer.Write(Data.Length);
        writer.Write(Data);
        return ms.ToArray();
    }

    public void Read(byte[] bytes)
    {
        var ms = new MemoryStream(bytes);
        var reader = new BinaryReader(ms);
        Type = (MessageType)reader.ReadByte();
        IsClose = reader.ReadBoolean();
        var dataLength = reader.ReadInt32();
        Data = reader.ReadBytes(dataLength);
    }
}