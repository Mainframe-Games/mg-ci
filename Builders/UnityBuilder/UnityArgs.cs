using System.Text;

namespace UnityBuilder;

internal readonly struct UnityArgs
{
    public string? BuildTarget { get; init; }
    public string? ProjectPath { get; init; }
    public string? ExecuteMethod { get; init; }
    public string? LogPath { get; init; }

    // public string? BuildPath { get; init; }
    public string? SubTarget { get; init; }

    /// <summary>
    /// Custom args specific to mg-ci
    /// </summary>
    public string[]? CustomArgs { get; init; }

    public string Build()
    {
        var args = new StringBuilder("-quit -batchmode");

        if (!string.IsNullOrEmpty(BuildTarget))
            args.Append($" -buildTarget {BuildTarget}");

        if (!string.IsNullOrEmpty(ProjectPath))
            args.Append($" -projectPath \"{ProjectPath}\"");

        if (!string.IsNullOrEmpty(ExecuteMethod))
            args.Append($" -executeMethod \"{ExecuteMethod}\"");

        if (!string.IsNullOrEmpty(LogPath))
            args.Append($" -logFile \"{LogPath}\"");

        // if (!string.IsNullOrEmpty(BuildPath))
        // args.Append($" -buildPath \"{BuildPath}\"");

        if (!string.IsNullOrEmpty(SubTarget))
            args.Append($" -standaloneBuildSubtarget \"{SubTarget}\"");

        if (CustomArgs is not null)
            foreach (var customArg in CustomArgs)
                args.Append($" {customArg}");

        var output = args.ToString();
        return output;
    }
}
