using System.Net;

namespace ClanforgeDeployment.Utils;

internal static class Web
{
    public static async Task<string> SendAsync(
        HttpMethod get,
        string url,
        string authToken,
        (HttpRequestHeader ContentType, string) headers
    )
    {
        throw new NotImplementedException();
    }
}
