using Newtonsoft.Json.Linq;

namespace MainServer.Services.Packets;

internal class BuildRunnerPacket
{
    public Guid ProjectGuid { get; set; }
    public string? TargetName { get; set; }
    public string? Branch { get; set; }

    public JObject ToJson()
    {
        return JObject.FromObject(this);
    }

    public override string ToString()
    {
        return ToJson().ToString();
    }
}
