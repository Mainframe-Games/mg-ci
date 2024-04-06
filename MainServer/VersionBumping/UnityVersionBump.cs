namespace MainServer.VersionBumping;

internal class UnityVersionBump(string projectPath, bool standalone, bool android, bool ios)
{
    public string ProjectSettingsPath { get; } =
        Path.Combine(projectPath, "ProjectSettings", "ProjectSettings.asset");

    /// <summary>
    /// Returns full version {bundle}.{standalone}
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NullReferenceException"></exception>
    public string Run()
    {
        var projectSettings = new UnityProjectSettings(ProjectSettingsPath);

        // set bundle version to same. It should be set by user
        var outBundle = projectSettings.GetBundleVersion() ?? throw new NullReferenceException();
        Console.WriteLine($"New BundleVersion: {outBundle}");
        projectSettings.WriteBundleVersion(outBundle);

        // standalone
        if (standalone)
        {
            var outStandalone = projectSettings.GetStandaloneBuildNumber() + 1;
            Console.WriteLine($"New Standalone: {outStandalone}");
            projectSettings.WritePlatformBuildNumber("Standalone", outStandalone);
        }

        // android
        if (android)
        {
            var outAndroid = projectSettings.GetAndroidBuildCode() + 1;
            Console.WriteLine($"New Android: {outAndroid}");
            projectSettings.WriteAndroidBundleVersionCode(outAndroid);
        }

        // iOS
        if (ios)
        {
            var outIos = projectSettings.GetIphoneBuildNumber() + 1;
            Console.WriteLine($"New iOS: {outIos}");
            projectSettings.WritePlatformBuildNumber("iPhone", outIos);
        }

        projectSettings.SaveFile();
        return $"{outBundle}.{standalone}";
    }

    private class UnityProjectSettings
    {
        private readonly string _path;
        private readonly string[] _lines;

        public UnityProjectSettings(string path)
        {
            _path = path;
            if (!File.Exists(path))
                throw new FileNotFoundException(path);

            _lines = File.ReadAllLines(path);
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

        private string GetValue(params string[] path)
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
}
