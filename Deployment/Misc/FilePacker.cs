using System.IO.Compression;
using SharedLib;

namespace Deployment.Misc;

/// <summary>
/// Class for packing a folder for sending over network
/// </summary>
public static class FilePacker
{
	private const uint MB = 1000000;
	
	/// <summary>
	/// 
	/// </summary>
	/// <param name="pathToDir"></param>
	/// <returns>Base64 string of zip file</returns>
	public static async Task<string> PackAsync(string? pathToDir)
	{
		var zipPath = $"{pathToDir}.zip";
		Delete(zipPath);
		Logger.Log($"Packing file... {zipPath}");
		ZipFile.CreateFromDirectory(pathToDir, zipPath);
		var fileBytes = await File.ReadAllBytesAsync(zipPath);
		Logger.Log($"Zip size: {fileBytes.Length / MB} {nameof(MB)}");
		// var compressBytes = GZip.Compress(fileBytes);
		// Logger.Log($"Zip size (compressed): {compressBytes.Length / MB} {nameof(MB)}");
		var base64 = Convert.ToBase64String(fileBytes);
		return base64;
	}
	
	public static async Task UnpackAsync(string? zipName, string? base64, string? destPathDir)
	{
		Delete(zipName);
		Delete(destPathDir);
		Logger.Log($"Unpacking file... '{zipName}' to '{destPathDir}'");
		var compressedBytes = Convert.FromBase64String(base64);
		// var fileBytes = GZip.Decompress(compressedBytes);
		await File.WriteAllBytesAsync(zipName, compressedBytes);
		ZipFile.ExtractToDirectory(zipName, destPathDir);
		Delete(zipName);
		Logger.Log("Unpacking file... done");
	}

	private static void Delete(string? path)
	{
		if (File.Exists(path))
			File.Delete(path);

		if (Directory.Exists(path))
			Directory.Delete(path, true);
	}
}