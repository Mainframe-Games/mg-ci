namespace ServerClientShared;

public enum MessageType : byte
{
    Connection,
    Disconnection,
    Message,
}

public class NetworkPayload(MessageType type, ushort clientId, object? data)
{
    public MessageType Type { get; set; } = type;
    public ushort ClientId { get; set; } = clientId;
    public object? Data { get; set; } = data;
}
