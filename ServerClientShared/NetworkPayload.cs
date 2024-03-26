namespace ServerClientShared;

public enum MessageType : byte
{
    Connection,
    Disconnection,
    Message,
}

public class NetworkPayload
{
    public MessageType Type { get; set; } = MessageType.Message;
    public ushort ClientId { get; set; }
    public object? Data { get; set; }
}
