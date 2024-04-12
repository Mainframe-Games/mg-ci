using System.Security.Cryptography;
using System.Text;

namespace SocketServer;

public static class CheckSum
{
    public static string Build(byte[] bytes)
    {
        var hashBytes = SHA256.HashData(bytes);
        var sb = new StringBuilder();
        foreach (var b in hashBytes)
            sb.Append(b.ToString("x2"));
        return sb.ToString();
    }
}