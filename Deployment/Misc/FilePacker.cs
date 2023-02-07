using System.Diagnostics;
using System.IO.Compression;
using SharedLib;

namespace Deployment.Misc;

/// <summary>
/// Class for packing a folder for sending over network
/// </summary>
public static class FilePacker
{
	/// <summary>
	/// 
	/// </summary>
	/// <param name="pathToDir"></param>
	/// <returns>Base64 string of zip file</returns>
	public static async Task<string> PackAsync(string pathToDir)
	{
		var zipPath = $"{pathToDir}.zip";
		
		if (File.Exists(zipPath))
			File.Delete(zipPath);
		
		Logger.Log($"Packing file... {zipPath}");
		ZipFile.CreateFromDirectory(pathToDir, zipPath);
		var fileBytes = await File.ReadAllBytesAsync(zipPath);
		var compressBytes = GZip.Compress(fileBytes);
		var base64 = Convert.ToBase64String(compressBytes);
		return base64;
	}
	
	public static async Task UnpackAsync(string? zipName, string? base64, string? destPath)
	{
		var compressedBytes = Convert.FromBase64String(base64);
		var fileBytes = GZip.Decompress(compressedBytes);
		await File.WriteAllBytesAsync(zipName, fileBytes);
		ZipFile.ExtractToDirectory(zipName, destPath);
	}
}