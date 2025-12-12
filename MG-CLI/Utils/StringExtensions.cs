using System.Text.RegularExpressions;

namespace MG_CLI;

public static partial class StringExtensions
{
    public static string ToSnakeCase(this string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "anim";

        // 1) Insert _ between camelCase boundaries
        var s = SnakeRegex1().Replace(input, "$1_$2");

        // 2) Replace separators with _
        s = SnakeRegex2().Replace(s, "_");

        // 3) Lowercase
        s = s.ToLowerInvariant();

        // 4) Collapse multiple _
        s = Regex.Replace(s, "_{2,}", "_");

        // 5) Trim
        s = s.Trim('_');

        return string.IsNullOrEmpty(s) ? "anim" : s;
    }

    [GeneratedRegex(@"([a-z0-9])([A-Z])", RegexOptions.Compiled)]
    private static partial Regex SnakeRegex1();
    [GeneratedRegex(@"[\s\-\.\:]+", RegexOptions.Compiled)]
    private static partial Regex SnakeRegex2();
}