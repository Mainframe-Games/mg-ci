using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace SocketServer.Messages;

public enum OperationSystemType
{
    Windows,
    MacOS,
    Linux
}

public class ServerConnectionMessage
{
    public uint ClientId { get; set; }
    public OperationSystemType OperatingSystem { get; set; }
    public string? MachineName { get; set; }
    public string[]? Services { get; set; }

    [JsonIgnore]
    private static readonly JsonSerializerSettings _settings =
        new()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
            Converters = { new StringEnumConverter(), }
        };

    public override string ToString()
    {
        return JsonConvert.SerializeObject(this, _settings);
    }

    public static ServerConnectionMessage Parse(string json)
    {
        try
        {
            return JsonConvert.DeserializeObject<ServerConnectionMessage>(json, _settings)
                ?? throw new NullReferenceException();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}
