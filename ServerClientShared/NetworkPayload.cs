namespace ServerClientShared;

public enum MessageType : byte
{
    Connection,
    Disconnection,
    Message,
    Error,
}

public class NetworkPayload(MessageType type, object? data)
{
    public MessageType Type { get; set; } = type;
    public object? Data { get; set; } = data;
}
