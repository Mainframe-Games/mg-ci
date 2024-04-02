using Newtonsoft.Json.Linq;

namespace Deployment.Server.Unity;

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
