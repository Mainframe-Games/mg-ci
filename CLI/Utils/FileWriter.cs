using System.Text;

namespace CLI.Utils;

public abstract class FileWriter
{
    /// <summary>
    /// Asynchronously writes a collection of strings to a file, overwriting the file if it exists.
    /// </summary>
    /// <param name="path">The file path where the lines will be written.</param>
    /// <param name="lines">An array of strings to write to the file.</param>
    /// <returns>A task that represents the asynchronous write operation.</returns>
    public static async Task WriteAllLinesAsync(string path, string[] lines)
    {
        await File.WriteAllLinesAsync(path, lines, new UTF8Encoding(false));
    }
}