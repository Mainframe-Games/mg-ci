using System.Diagnostics;
using System.Text;

namespace SharedLib;

public static class Cmd
{
	public static (int exitCode, string output) Run(string fileName, string ags, bool redirectOutput = true, bool logOutput = true)
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
				Arguments = ags
			};

			var proc = Process.Start(procStartInfo);
			var sb = new StringBuilder();
			proc.ErrorDataReceived += (_, e) => { Write(sb, e.Data, logOutput); };
			
			if (procStartInfo.RedirectStandardOutput)
			{
				proc.OutputDataReceived += (_, e) => { Write(sb, e.Data, logOutput); };
				proc.BeginOutputReadLine();
			}
			
			proc.BeginErrorReadLine();
			proc.WaitForExit();
			var code = proc.ExitCode;
			var output = sb.ToString().Trim();
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
			sb.AppendLine(str);
			if (logOutput)
				Logger.Log(str);
		}
		catch (Exception e)
		{
			Logger.Log(e);
		}
	}
	
	public static int Choose(string remark, List<string?> options)
	{
		// choose
		var str = new StringBuilder();
		str.AppendLine("");
		for (int i = 0; i < options.Count; i++)
			str.AppendLine($"[{i}] {options[i]}");
			
		Logger.Log(str.ToString());
		Console.Write($"{remark} [0..{options.Count - 1}] ");
		var stdIn = Console.ReadLine();
		var index = int.TryParse(stdIn, out int outIndex) ? outIndex : 0;
		return index;
	}

	public static bool Ask(string question, bool defaultAnswer)
	{
		var yn = defaultAnswer ? "[Y/n]" : "[y/N]";
		Console.Write($"{question} {yn}: ");
		var response = Console.ReadLine();
		return string.IsNullOrEmpty(response)
			? defaultAnswer
			: response.ToLower() == "y";
	}
}