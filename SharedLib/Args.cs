namespace SharedLib;

public class Args
{
	public static readonly Args Environment = new(System.Environment.GetCommandLineArgs());
	
	private readonly List<string> _args = new();
	private readonly Dictionary<string, List<string>> _cmds = new();

	public Args(string[]? args)
	{
		_args.AddRange(args ?? Array.Empty<string>());
		var cmd = string.Empty;

		foreach (var arg in _args)
			AddCmd(arg, ref cmd);
	}

	private void AddCmd(string arg, ref string cmd)
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
	public bool TryGetArgs(string cmd, out List<string> args)
		=> _cmds.TryGetValue(cmd, out args);

	public bool TryGetArg(string cmd, out string arg, string defaultValue = null)
	{
		arg = defaultValue;

		if (!TryGetArgs(cmd, out var args) || args.Count <= 0)
			return false;

		arg = args[0];
		return true;
	}

	public bool TryGetArg(string cmd, int index, out string arg, string defaultValue = null)
	{
		arg = defaultValue;
		return TryGetArgs(cmd, out var args) && TryGetArg(args, index, out arg, defaultValue);
	}

	public bool TryGetArg(string cmd, int index, out int arg, int defaultValue = -1)
	{
		arg = defaultValue;
		return TryGetArgs(cmd, out var args) && TryGetArg(args, index, out arg, defaultValue);
	}

	public bool TryGetArg(IReadOnlyList<string> args, int index, out string arg, string defaultValue = null)
	{
		arg = defaultValue;

		if (args.Count == 0 || index < 0 || index >= args.Count)
			return false;

		arg = args[index];
		return true;
	}

	public bool TryGetArg(IReadOnlyList<string> args, int index, out int arg, int defaultValue = -1)
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
	public bool ContainsArg(string cmd, string arg)
		=> TryGetArgs(cmd, out var args) && args.Contains(arg);

	/// <summary>
	/// Returns if the commands is a boolean flag, i.e no args
	/// </summary>
	/// <param name="cmd"></param>
	/// <param name="zeroArgs"></param>
	/// <returns></returns>
	public bool IsFlag(string cmd, bool zeroArgs = true)
	{
		if (!TryGetArgs(cmd, out var args))
			return false;

		if (zeroArgs)
			return args.Count == 0;

		return true;
	}
}