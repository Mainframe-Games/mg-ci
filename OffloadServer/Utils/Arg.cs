namespace OffloadServer.Utils;

internal static class Arg
{
    private static readonly string[] _args = Environment.GetCommandLineArgs();

    public static string? GetArg(string name)
    {
        for (int i = 0; i < _args.Length; i++)
        {
            if (_args[i] == name && _args.Length > i + 1)
                return _args[i + 1];
        }

        return null;
    }

    public static bool IsFlag(string name)
    {
        return _args.Contains(name);
    }
}
