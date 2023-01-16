using System.Diagnostics;
using System.Text;

namespace Deployment.Misc;

public static class Cmd
{
	public static (int exitCode, string output) Run(string fileName, string ags, bool logOutput = true)
	{
		if (logOutput)
			Console.WriteLine($"{fileName} {ags}");

		try
		{
			var procStartInfo = new ProcessStartInfo(fileName)
			{
				RedirectStandardError = true,
				RedirectStandardOutput = true,
				RedirectStandardInput = true,
				UseShellExecute = false,
				CreateNoWindow = true,
				WorkingDirectory = Environment.CurrentDirectory,
				Arguments = ags
			};

			var proc = Process.Start(procStartInfo);
			var sb = new StringBuilder();
			proc.OutputDataReceived += (_, e) => { Write(sb, e.Data, logOutput); };
			proc.ErrorDataReceived += (_, e) => { Write(sb, e.Data, logOutput); };
			proc.BeginOutputReadLine();
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
		if (str == null)
			return;
		
		var trimmed = str.Trim();
		sb.AppendLine(trimmed);
		
		if (logOutput)
			Console.WriteLine(trimmed);
	}
	
	public static int Choose(string remark, IReadOnlyList<string> options)
	{
		// choose
		for (int i = 0; i < options.Count; i++)
			Console.WriteLine($"[{i}] {options[i]}");

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