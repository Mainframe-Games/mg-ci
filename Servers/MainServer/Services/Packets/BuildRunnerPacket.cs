using Newtonsoft.Json.Linq;

namespace MainServer.Services.Packets;

internal class BuildRunnerPacket
{
    public Guid ProjectGuid { get; set; }
    public List<string> BuildTargets { get; set; } = [];
    public string GitUrl { get; set; } = string.Empty;
    public string Branch { get; set; } = string.Empty;

    public JObject ToJson()
    {
        return JObject.FromObject(this);
    }

    public override string ToString()
    {
        return ToJson().ToString();
    }
}
