using System.Text;
using CliWrap;
using CliWrap.Buffered;
using Spectre.Console;

namespace MG;

public static class FileEx
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

    /// <summary>
    /// Sets executable permissions on a file for the user, group, and others on Unix-based systems.
    /// </summary>
    /// <param name="filePath">The path of the file for which the executable permissions will be set.</param>
    public static void SetExecutablePermissionsUnix(in string filePath)
    {
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS())
            return;
        
        var res = Cli.Wrap("chmod")
            .WithArguments($"+x {filePath}")
            .ExecuteBufferedAsync();
        
        res.Task.Wait();
        var result = res.Task.Result;
        if (result.ExitCode != 0)
        {
            Log.WriteLine(result.StandardError, Color.Red);
        }
        else
        {
            Log.WriteLine(result.StandardOutput);
        }
        
        // var currentModes = File.GetUnixFileMode(filePath);
        //
        // var newMode = currentModes 
        //               | UnixFileMode.UserExecute
        //               | UnixFileMode.GroupExecute
        //               | UnixFileMode.OtherExecute;
        //
        // File.SetUnixFileMode(filePath, newMode);
    }
}