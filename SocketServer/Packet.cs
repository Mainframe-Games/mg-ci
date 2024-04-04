namespace SocketServer;

internal enum PacketType : byte
{
    Server,
    Client,
}

internal class Packet
{
    private static uint NextId;
    public uint Id { get; private set; } = NextId++;
    public uint TotalBytes { get; set; }
    public uint FragmentCount { get; set; }
    public uint FragmentIndex { get; set; }
    public byte[] Fragments { get; set; }

    public byte[] GetBytes()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        writer.Write(Id); // uint32
        writer.Write(TotalBytes); // uint32
        writer.Write(FragmentCount); // uint32
        writer.Write(FragmentIndex); // uint32
        writer.Write(Fragments.Length); // int32
        writer.Write(Fragments); // byte[]

        return ms.ToArray();
    }

    public void ReadBytes(byte[] bytes)
    {
        using var ms = new MemoryStream(bytes);
        using var reader = new BinaryReader(ms);

        Id = reader.ReadUInt32();
        TotalBytes = reader.ReadUInt32();
        FragmentCount = reader.ReadUInt32();
        FragmentIndex = reader.ReadUInt32();

        var fragmentLength = reader.ReadInt32();
        Fragments = reader.ReadBytes(fragmentLength);
    }

    public static byte[] TestSend()
    {
        var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        var array = new byte[Config.MB * 500];
        writer.Write(array.Length);
        var rand = new Random();
        for (int i = 0; i < array.Length; i++)
        {
            array[i] = (byte)rand.Next(0, 255);
            writer.Write(array[i]);
        }
        
        Console.WriteLine($"[Client] - {array[^1]}");

        return ms.ToArray();
    }

    public static void TestReceive(byte[] bytes)
    {
        var ms = new MemoryStream(bytes);
        using var reader = new BinaryReader(ms);

        var strArrayLength = reader.ReadInt32();
        var str = 0;
        for (int i = 0; i < strArrayLength; i++)
        {
            if( reader.BaseStream.Position == reader.BaseStream.Length )
                break;
            
            str = reader.ReadByte();
        }
        
        Console.WriteLine(str);
    }
}