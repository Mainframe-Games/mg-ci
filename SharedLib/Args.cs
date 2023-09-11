﻿namespace SharedLib;

public class Args
{
	public static readonly Args Environment = new(System.Environment.GetCommandLineArgs());
	private readonly Dictionary<string, List<string>> _cmds = new();

	public Args(string[]? args)
	{
		var _args = new List<string>();
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
	
	public void Add(string flag, string? arg = null)
	{
		if (!flag.StartsWith('-'))
			throw new Exception("Flags must start with -");
		
		// add flag
		if (!_cmds.ContainsKey(flag))
			_cmds[flag] = new List<string>();
		
		// add arg
		if (!string.IsNullOrEmpty(arg))
			_cmds[flag].Add(arg);
	}

	/// <summary>
	/// Returns all command args from a command if present
	/// </summary>
	/// <param name="cmd"></param>
	/// <param name="args"></param>
	/// <returns></returns>
	private bool TryGetArgs(string cmd, out List<string> args)
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

	private static bool TryGetArg(IReadOnlyList<string> args, int index, out string arg, string defaultValue = null)
	{
		arg = defaultValue;

		if (args.Count == 0 || index < 0 || index >= args.Count)
			return false;

		arg = args[index];
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
	/// Returns if flag exists, can have args or no args, it just checks for flag
	/// </summary>
	/// <param name="cmd"></param>
	/// <returns></returns>
	public bool IsFlag(string cmd)
	{
		return TryGetArgs(cmd, out _);
	}
}