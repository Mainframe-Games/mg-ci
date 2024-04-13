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

    public static IList<T>? GetList<T>(this TomlTable table, string key)
    {
        if (table.TryGetValue(key, out var value))
        {
            if (value is TomlArray array)
                return new List<T>(array.ToList().Cast<T>());
        }
        return null;
    }

    public static IList<T>? GetList<T>(this TomlTable table, params string[] keys)
    {
        var root = table;

        foreach (var key in keys)
        {
            if (!root.TryGetValue(key, out var value))
                continue;

            if (value is TomlTable subTable)
                root = subTable;
            else if (value is TomlArray array)
                return new List<T>(array.ToList().Cast<T>());
        }

        return default;
    }
}
