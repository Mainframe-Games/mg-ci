using System.Net;
using System.Text;
using Newtonsoft.Json.Linq;

namespace SharedLib;

public static class Web
{
    public struct Response
    {
        public HttpStatusCode StatusCode;
        public string Content;

        public override string ToString()
        {
            return $"({StatusCode}) {Content}";
        }
    }

    public static async Task<Response> SendAsync(
        HttpMethod method,
        string? url,
        string? authToken = null,
        object? body = null,
        params (HttpRequestHeader key, string value)[] headers
    )
    {
        if (url == null)
            throw new NullReferenceException("Url can not be null");

        using var client = new HttpClient();
        var uri = new Uri(url);
        var msg = new HttpRequestMessage
        {
            Method = method,
            RequestUri = uri,
            Headers = { { HttpRequestHeader.Accept.ToString(), "application/json" }, }
        };

        if (!string.IsNullOrEmpty(authToken))
            msg.Headers.Add(HttpRequestHeader.Authorization.ToString(), authToken);

        foreach (var header in headers)
            msg.Headers.Add(header.key.ToString(), header.value);

        var jsonBody = string.Empty;

        if (body != null)
        {
            jsonBody = Json.Serialise(body);
            msg.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
        }

        var jsonBodyLog = jsonBody.Length > 1000 ? $"{jsonBody[..1000]}...[truncated]" : jsonBody;
        Logger.Log($"HTTP_SEND {method.ToString().ToUpper()} {url}\n{jsonBodyLog}");

        try
        {
            using var res = await client.SendAsync(msg);
            var content = await res.Content.ReadAsStringAsync();

            if (res.IsSuccessStatusCode)
            {
                return new Response { StatusCode = res.StatusCode, Content = content };
            }

            var errorContent = new JObject { ["reason"] = res.ReasonPhrase, ["content"] = content };
            Logger.Log($"Response Failed ({res.StatusCode}): {errorContent}");

            return new Response { StatusCode = res.StatusCode, Content = errorContent.ToString() };
        }
        catch (Exception e)
        {
            Logger.Log(e);
            return new Response
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = $"{e.GetType().Namespace}: {e.Message}"
            };
        }
    }
}
