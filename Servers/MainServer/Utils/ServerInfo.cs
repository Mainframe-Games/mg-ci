using System.Reflection;

namespace MainServer.Utils;

internal class ServerInfo
{
    public static string Version { get; } = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0.0";
}