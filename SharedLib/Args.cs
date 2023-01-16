namespace SharedLib;

public class Args
{
	private static readonly List<string> _args = new();
	private static readonly Dictionary<string, List<string>> _cmds = new();

	static Args()
	{
		_args.AddRange(Environment.GetCommandLineArgs());
		var cmd = string.Empty;

		foreach (var arg in _args)
			AddCmd(arg, ref cmd);
	}

	private static void AddCmd(string arg, ref string cmd)
	{
		if (arg.StartsWith('-'))
		{
			// command
			cmd = arg;
			_cmds[cmd] = new List<string>();
		}
		else if (!string.IsNullOrEmpty(cmd))
		{
			// args
			_cmds[cmd].Add(arg);
		}
	}

	/// <summary>
	/// Returns all command args from a command if present
	/// </summary>
	/// <param name="cmd"></param>
	/// <param name="args"></param>
	/// <returns></returns>
	public static bool TryGetArgs(string cmd, out List<string> args)
		=> _cmds.TryGetValue(cmd, out args);

	public static bool TryGetArg(string cmd, out string arg, string defaultValue = null)
	{
		arg = defaultValue;

		if (!TryGetArgs(cmd, out var args) || args.Count <= 0)
			return false;

		arg = args[0];
		return true;
	}

	public static bool TryGetArg(string cmd, int index, out string arg, string defaultValue = null)
	{
		arg = defaultValue;
		return TryGetArgs(cmd, out var args) && TryGetArg(args, index, out arg, defaultValue);
	}

	public static bool TryGetArg(string cmd, int index, out int arg, int defaultValue = -1)
	{
		arg = defaultValue;
		return TryGetArgs(cmd, out var args) && TryGetArg(args, index, out arg, defaultValue);
	}

	public static bool TryGetArg(IReadOnlyList<string> args, int index, out string arg, string defaultValue = null)
	{
		arg = defaultValue;

		if (args.Count == 0 || index < 0 || index >= args.Count)
			return false;

		arg = args[index];
		return true;
	}

	public static bool TryGetArg(IReadOnlyList<string> args, int index, out int arg, int defaultValue = -1)
	{
		arg = defaultValue;
		if (!TryGetArg(args, index, out string str, defaultValue.ToString()))
			return false;
		arg = Convert.ToInt32(str);
		return true;
	}

	/// <summary>
	/// Returns if arg is present in command arguments
	/// </summary>
	/// <param name="cmd"></param>
	/// <param name="arg"></param>
	/// <returns></returns>
	public static bool ContainsArg(string cmd, string arg)
		=> TryGetArgs(cmd, out var args) && args.Contains(arg);

	/// <summary>
	/// Returns if the commands is a boolean flag, i.e no args
	/// </summary>
	/// <param name="cmd"></param>
	/// <returns></returns>
	public static bool IsFlag(string cmd)
		=> TryGetArgs(cmd, out var args) && args.Count == 0;
}