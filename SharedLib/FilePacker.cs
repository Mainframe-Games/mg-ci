using System.IO.Compression;
using System.Text;

namespace SharedLib;

/// <summary>
/// Class for packing a folder for sending over network
/// </summary>
public static class FilePacker
{
	public static async Task<Entry[]> PackRawAsync(string? pathToDir)
	{
		var zipPath = $"{pathToDir}.zip";
		Delete(zipPath);
		Logger.Log($"Packing file... {zipPath}");
		ZipFile.CreateFromDirectory(pathToDir, zipPath);
		var fileBytes = await ReadLargeZipFile(zipPath);
		Logger.Log($"Zip size: {fileBytes.ToByteSizeString()}");
		return fileBytes;
	}
	
	public static async Task UnpackRawAsync(string? zipName, Entry[] entries, string? destPathDir)
	{
		Delete(zipName);
		Delete(destPathDir);
		Logger.Log($"Unpacking file... '{zipName}' to '{destPathDir}'");
		await WriteLargeZipFile(zipName, entries);
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

	private static async Task<Entry[]> ReadLargeZipFile(string zipFilePath)
	{
		var frags = new List<Entry>();
		await using var fileStream = new FileStream(zipFilePath, FileMode.Open, FileAccess.Read);
		using var zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Read);

		var progress = new ProgressBar();

		for (var i = 0; i < zipArchive.Entries.Count; i++)
		{
			var entry = zipArchive.Entries[i];
			
			progress.SetContext($"Reading entry... {entry.FullName}");
			progress.Report(i / (double)zipArchive.Entries.Count);
			
			using var ms = new MemoryStream();
			await using var entryStream = entry.Open();
			await entryStream.CopyToAsync(ms);
			frags.Add(new Entry(entry.FullName, ms.ToArray()));
		}

		progress.Dispose();
		return frags.ToArray();
	}

	private static async Task WriteLargeZipFile(string zipFilePath, Entry[] entries)
	{
		await using var fileStream = new FileStream(zipFilePath, FileMode.Create, FileAccess.Write);
		using var zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Create);

		var progress = new ProgressBar();

		for (var i = 0; i < entries.Length; i++)
		{
			var entry = zipArchive.CreateEntry(entries[i].Name, CompressionLevel.Optimal);
			await using var entryStream = entry.Open();

			progress.SetContext($"Reading entry... {entry.FullName}");
			progress.Report(i / (double)entries.Length);

			await entryStream.WriteAsync(entries[i].Bytes);
		}
		
		progress.Dispose();
	}
	
	public struct Entry
	{
		public string Name;
		public byte[] Bytes;
		
		public ulong SizeOf => (ulong)
			(Encoding.UTF8.GetByteCount(Name)
			 + Bytes.Length);

		public Entry(string name, byte[] bytes)
		{
			Name = name;
			Bytes = bytes;
		}

		public void Write(BinaryWriter writer)
		{
			writer.Write(Name);
			writer.Write(Bytes.Length);
			writer.Write(Bytes);
		}
	
		public void Read(BinaryReader reader)
		{
			Name = reader.ReadString();

			var length = reader.ReadInt32();
			Bytes = reader.ReadBytes(length);
		}
	}
}