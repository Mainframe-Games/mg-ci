using Spectre.Console;

namespace CLI.Utils;

public static class Web
{
    public static async Task DownloadFileWithProgressAsync(string url, string destinationPath)
    {
        await AnsiConsole.Progress().StartAsync(async ctx =>
        {
            var fileName = Path.GetFileName(url);
            var prog = ctx.AddTask($"Downloading: {fileName}");
            
            using var httpClient = new HttpClient();
            using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            var bytesRead = 0L;
    
            await using var contentStream = await response.Content.ReadAsStreamAsync();
            await using var fileStream = File.Create(destinationPath);
    
            var buffer = new byte[8192];
            var isMoreToRead = true;

            do
            {
                var read = await contentStream.ReadAsync(buffer);
                if (read == 0)
                {
                    isMoreToRead = false;
                }
                else
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, read));
                    bytesRead += read;

                    if (totalBytes != -1L)
                    {
                        var progressPercentage = (double)bytesRead / totalBytes * 100;
                        prog.Value(progressPercentage);
                    }
                }
            }
            while (isMoreToRead);
            
        });
        

    }
}