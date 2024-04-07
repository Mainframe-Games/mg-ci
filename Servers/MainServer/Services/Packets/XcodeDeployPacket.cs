using Newtonsoft.Json.Linq;

namespace MainServer.Services.Packets;

public class XcodeDeployPacket
{
    public Guid ProjectGuid { get; set; }
    public string? AppleId { get; set; }
    public string? AppSpecificPassword { get; set; }

    public JObject ToJson()
    {
        return JObject.FromObject(this);
    }

    public override string ToString()
    {
        return ToJson().ToString();
    }
}
