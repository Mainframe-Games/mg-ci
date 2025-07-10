using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SharedLib;

public static class Cmd
{
    public static readonly List<Process> Processes = new();

    /// <summary>
    /// Kills all active processes
    /// </summary>
    /// <param name="entireProcessTree"></param>
    public static void KillAll(bool entireProcessTree = true)
    {
        foreach (var process in Processes)
        {
            Logger.Log($"Killing process: {process.ProcessName}");
            process.Kill(entireProcessTree);
        }
        Processes.Clear();
    }

    public static (int exitCode, string output) Run(
        string fileName,
        string ags,
        bool redirectOutput = true,
        bool logOutput = true
    )
    {
        if (logOutput)
            Logger.Log($"[CMD] {fileName} {ags}");

        try
        {
            var procStartInfo = new ProcessStartInfo(fileName)
            {
                RedirectStandardError = true,
                RedirectStandardOutput = redirectOutput,
                // RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Environment.CurrentDirectory,
                Arguments = ags,
            };

            var proc = Process.Start(procStartInfo);
            // Logger.Log($"Process Started: {proc.ProcessName}");
            Processes.Add(proc);

            var sb = new StringBuilder();
            proc.ErrorDataReceived += (_, e) =>
            {
                Write(sb, e.Data, logOutput);
            };

            if (procStartInfo.RedirectStandardOutput)
            {
                proc.OutputDataReceived += (_, e) =>
                {
                    Write(sb, e.Data, logOutput);
                };
                proc.BeginOutputReadLine();
            }

            proc.BeginErrorReadLine();
            proc.WaitForExit();

            var code = proc.ExitCode;
            var output = sb.ToString().Trim();

            Processes.Remove(proc);

            return (code, output);
        }
        catch (Exception e)
        {
            return (1, $"Error in command: {ags}, {e.Message}");
        }
    }

    public static (int exitCode, string output) Run(
        string fileName,
        string ags,
        string workingDirectory,
        bool redirectOutput = true,
        bool logOutput = true
    )
    {
        if (logOutput)
            Logger.Log($"[CMD] {fileName} {ags}");

        try
        {
            var procStartInfo = new ProcessStartInfo(fileName)
            {
                RedirectStandardError = true,
                RedirectStandardOutput = redirectOutput,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory,
                Arguments = ags,
            };

            var proc = Process.Start(procStartInfo);
            // Logger.Log($"Process Started: {proc.ProcessName}");
            Processes.Add(proc);

            var sb = new StringBuilder();
            proc.ErrorDataReceived += (_, e) =>
            {
                Write(sb, e.Data, logOutput);
            };

            if (procStartInfo.RedirectStandardOutput)
            {
                proc.OutputDataReceived += (_, e) =>
                {
                    Write(sb, e.Data, logOutput);
                };
                proc.BeginOutputReadLine();
            }

            proc.BeginErrorReadLine();
            proc.WaitForExit();

            var code = proc.ExitCode;
            var output = sb.ToString().Trim();

            Processes.Remove(proc);

            return (code, output);
        }
        catch (Exception e)
        {
            return (1, $"Error in command: {ags}, {e.Message}");
        }
    }

    private static void Write(StringBuilder sb, string? str, bool logOutput)
    {
        try
        {
            if (!string.IsNullOrEmpty(str))
                sb.AppendLine(str);

            if (logOutput)
                Logger.Log(str);
        }
        catch (Exception e)
        {
            Logger.Log(e);
            Logger.Log(str);
        }
    }

    public static bool Choose(string remark, List<string?> options, out int index)
    {
        // choose
        var str = new StringBuilder();
        str.AppendLine("");
        for (int i = 0; i < options.Count; i++)
            str.AppendLine($"[{i}] {options[i]}");

        Logger.Log(str.ToString());
        Console.Write($"{remark} [0..{options.Count - 1}] ");
        var stdIn = Console.ReadLine();
        index = int.TryParse(stdIn, out int outIndex) ? outIndex : -1;
        return index != -1;
    }

    public static bool Ask(string question, bool defaultAnswer)
    {
        var yn = defaultAnswer ? "[Y/n]" : "[y/N]";
        Console.Write($"{question} {yn}: ");
        var response = Console.ReadLine();
        return string.IsNullOrEmpty(response) ? defaultAnswer : response.ToLower() == "y";
    }

    public static int Ask(string question, int defaultAnswer)
    {
        Console.Write($"{question} [{defaultAnswer}]: ");
        var response = Console.ReadLine();

        if (string.IsNullOrEmpty(response))
            return defaultAnswer;

        return int.TryParse(response, out var integer) ? integer : defaultAnswer;
    }

    public static string Ask(string question, string defaultAnswer)
    {
        Console.Write($"{question} [{defaultAnswer}]: ");
        var response = Console.ReadLine();

        if (string.IsNullOrEmpty(response))
            return defaultAnswer;

        return response;
    }
}
