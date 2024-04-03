using Newtonsoft.Json.Linq;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace OffloadServer;

internal class ConnectService : WebSocketBehavior
{
    private static string OsName
    {
        get
        {
            if (OperatingSystem.IsWindows())
                return "windows";
            if (OperatingSystem.IsMacOS())
                return "macos";
            if (OperatingSystem.IsLinux())
                return "linux";
            throw new NotSupportedException("Operating system not supported");
        }
    }

    /// <summary>
    /// Array of services provided by offload server
    /// </summary>
    public static string[]? Services { get; set; }

    protected override void OnOpen()
    {
        base.OnOpen();
        Console.WriteLine($"Client connected [{Context.RequestUri.AbsolutePath}]: {ID}");

        Send(new JObject
        {
            ["OperatingSystem"] = OsName,
            ["Status"] = "Connection",
            ["Services"] = JArray.FromObject(Services ?? throw new NullReferenceException()),
        }.ToString());
    }

    protected override void OnClose(CloseEventArgs e)
    {
        base.OnClose(e);
        Console.WriteLine($"Client disconnected [{Context.RequestUri.AbsolutePath}]: {ID}");
    }
}