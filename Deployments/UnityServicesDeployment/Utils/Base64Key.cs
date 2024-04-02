using System.Text;

namespace UnityServicesDeployment.Utils;

internal class Base64Key
{
    public static string Generate(string accessKey, string secretKey)
    {
        var bytes = Encoding.UTF8.GetBytes($"{accessKey}:{secretKey}");
        var base64 = Convert.ToBase64String(bytes);
        return base64;
    }
}
