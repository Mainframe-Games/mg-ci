using Tomlyn.Model;

namespace MainServer.Utils;

internal static class TomlEx
{
    public static T? GetValue<T>(this TomlTable table, params string[] keys)
    {
        var root = table;

        foreach (var key in keys)
        {
            if (!root.TryGetValue(key, out var value))
                continue;

            if (value is TomlTable subTable)
                root = subTable;
            else
                return (T)value;
        }

        return default;
    }

    public static T? GetValue<T>(this TomlTable table, string key)
    {
        if (table.TryGetValue(key, out var value))
            return (T)value;
        return default;
    }
}
