namespace Utils.Unity;

internal class UnityProjectSettings
{
    private readonly string _path;
    private readonly string[] _lines;

    public UnityProjectSettings(string projectPath)
    {
        var assetPath = Path.Combine(projectPath, "ProjectSettings", "ProjectSettings.asset");

        _path = assetPath;

        if (!File.Exists(assetPath))
            throw new FileNotFoundException(assetPath);

        _lines = File.ReadAllLines(assetPath);
    }

    public bool IsIL2CPP_Standalone()
    {
        return GetValue("scriptingBackend", "Standalone") == "1";
    }

    public bool IsIL2CPP_Server()
    {
        return GetValue("scriptingBackend", "Server") == "1";
    }

    public int GetStandaloneBuildNumber()
    {
        var value = GetValue("buildNumber", "Standalone");
        return int.Parse(value);
    }

    public int GetAndroidBuildCode()
    {
        var value = GetValue("AndroidBundleVersionCode");
        return int.Parse(value);
    }

    public int GetIphoneBuildNumber()
    {
        var value = GetValue("buildNumber", "iPhone");
        return int.Parse(value);
    }

    public void SaveFile()
    {
        File.WriteAllLines(_path, _lines);
    }

    public string GetBundleVersion()
    {
        return GetValue("bundleVersion");
    }

    public void WriteBundleVersion(string? newBundleVersion)
    {
        var index = GetLineIndex("bundleVersion");
        _lines[index] = ReplaceText(index, newBundleVersion);
    }

    public void WritePlatformBuildNumber(string platform, int newBundleVersion)
    {
        var index = GetLineIndex("buildNumber", platform);
        _lines[index] = ReplaceText(index, newBundleVersion.ToString());
    }

    public void WriteAndroidBundleVersionCode(int androidVersionCode)
    {
        var index = GetLineIndex("AndroidBundleVersionCode");
        _lines[index] = ReplaceText(index, androidVersionCode.ToString());
    }

    #region Utils

    public string GetValue(params string[] path)
    {
        var line = GetLine(path);
        var value = line.Split(':')[^1].Trim();
        return value;
    }

    private string GetLine(params string[] path)
    {
        var index = GetLineIndex(path);
        return _lines[index];
    }

    private int GetLineIndex(params string[] path)
    {
        var pathIndex = 0;

        for (var i = 0; i < _lines.Length; i++)
        {
            var line = _lines[i];
            if (line.Contains(path[pathIndex]))
                pathIndex++;

            if (pathIndex == path.Length)
                return i;
        }

        throw new Exception("Failed to find path: " + string.Join(", ", path));
    }

    private string ReplaceText(int lineIndex, string? newValue)
    {
        var oldValue = _lines[lineIndex].Split(":")[^1].Trim();

        if (string.IsNullOrEmpty(oldValue))
            throw new NullReferenceException(
                $"{nameof(oldValue)} is null on line '{_lines[lineIndex]}'"
            );

        var replacement = _lines[lineIndex].Replace(oldValue, newValue);
        return replacement;
    }

    #endregion
}
