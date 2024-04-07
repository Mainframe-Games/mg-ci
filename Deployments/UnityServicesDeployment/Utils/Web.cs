using Newtonsoft.Json.Linq;

namespace UnityServicesDeployment.Utils;

public static class Web
{
    public static async Task<string> SendAsync(
        HttpMethod put,
        string url,
        string? authToken,
        JObject? body = null
    )
    {
        throw new NotImplementedException();
    }
}
