using System.Security.Cryptography;
using System.Text;

namespace Deployment.Misc;

public sealed class DeviceInfo
{
	private static DeviceInfo Instance { get; } = new();
	private string Id { get; }
	
	/// <summary>
	/// Unique ID used to limit who can use this server
	/// </summary>
	public static string UniqueDeviceId => Instance.Id;

	private DeviceInfo()
	{
		var str = Environment.MachineName
		          + Environment.UserName
		          + Environment.OSVersion.Platform;

		using var md5 = MD5.Create();
		var inputBytes = Encoding.ASCII.GetBytes(str);
		var hashBytes = md5.ComputeHash(inputBytes);
		Id = Convert.ToHexString(hashBytes);
	}
}