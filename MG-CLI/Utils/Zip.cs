using System.IO.Compression;
using Spectre.Console;

namespace MG;

public static class Zip
{
    public static async Task UnzipFileAsync(string zipPath, string extractPath, int bufferSize = 81920)
    {
        await AnsiConsole.Progress()
            .StartAsync(async ctx =>
        {
            var fileName = Path.GetFileName(zipPath);
            var task = ctx.AddTask($"Unzipping: {fileName}");
            
            using var archive = ZipFile.OpenRead(zipPath);
            var totalBytes = archive.Entries.Sum(e => e.Length);
            var extractedBytes = 0L;

            foreach (var entry in archive.Entries)
            {
                var fullPath = Path.Combine(extractPath, entry.FullName);
                var directory = Path.GetDirectoryName(fullPath);
        
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                // Skip if it's a directory
                if (string.IsNullOrEmpty(entry.Name)) 
                    continue;

                // Extract file with buffering
                await using var entryStream = entry.Open();
                await using var fileStream = File.Create(fullPath);
                await entryStream.CopyToAsync(fileStream, bufferSize);
                
                extractedBytes += entry.Length;
                var progressPercentage = (double)extractedBytes / totalBytes * 100;
                task.Value(progressPercentage);
            }
        });
    } 
}