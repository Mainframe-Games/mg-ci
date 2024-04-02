using Newtonsoft.Json.Linq;

namespace OffloadServer;

internal class ConnectService : ServiceBase
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
    
    protected override void OnOpen()
    {
        base.OnOpen();
        Console.WriteLine($"Client connected [{Context.RequestUri.AbsolutePath}]: {ID}");

        var data = new JObject
        {
            ["OperationSystem"] = OsName,
            ["Services"] = new JArray("version-bump", "build"),
        };
        
        Send(data.ToString());
    }
}