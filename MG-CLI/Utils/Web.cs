using Spectre.Console;

namespace MG;

/// <summary>
/// Provides utility methods for web-related operations, such as downloading files and performing HTTP POST requests.
/// </summary>
public static class Web
{
    /// <summary>
    /// Downloads a file from the specified URL to the specified destination path and displays a progress bar during the download.
    /// </summary>
    /// <param name="url">The URL of the file to download.</param>
    /// <param name="destinationPath">The local file path where the downloaded file will be saved.</param>
    /// <returns>A task that represents the asynchronous download operation.</returns>
    /// <exception cref="HttpRequestException">Thrown when the HTTP request fails or the response status code indicates an error.</exception>
    /// <exception cref="IOException">Thrown when an I/O error occurs during file writing operations.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when the application lacks necessary permissions to write to the destination path.</exception>
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