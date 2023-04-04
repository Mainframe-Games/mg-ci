using System.IO.Compression;

namespace SharedLib;

/// <summary>
/// Class for packing a folder for sending over network
/// </summary>
public static class FilePacker
{
	public static async Task<byte[]> PackRawAsync(string? pathToDir)
	{
		var zipPath = $"{pathToDir}.zip";
		Delete(zipPath);
		Logger.Log($"Packing file... {zipPath}");
		ZipFile.CreateFromDirectory(pathToDir, zipPath);
		var fileBytes = await File.ReadAllBytesAsync(zipPath);
		Logger.Log($"Zip size: {fileBytes.ToMegaByteString()}");
		return fileBytes;
	}
	
	public static async Task UnpackRawAsync(string? zipName, byte[] data, string? destPathDir)
	{
		Delete(zipName);
		Delete(destPathDir);
		Logger.Log($"Unpacking file... '{zipName}' to '{destPathDir}'");
		await File.WriteAllBytesAsync(zipName, data);
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