namespace UnityBuilder;

internal static class UnityPath
{
    /// <summary>
    ///
    /// </summary>
    /// <param name="unityVersion"></param>
    /// <param name="useIntel">(Mac Server Only) Needs to be true for linux server builds on a mac</param>
    /// <returns></returns>
    public static string GetDefaultUnityPath(string? unityVersion, bool useIntel = false)
    {
        if (OperatingSystem.IsWindows())
            return $@"C:\Program Files\Unity\Hub\Editor\{unityVersion}\Editor\Unity.exe";

        if (OperatingSystem.IsLinux())
            throw new NotImplementedException("Linux builder not supported yet");

        // this only matters for linux builds on a mac server using IL2CPP, it needs to use Intel version of editor
        var x86_64 = useIntel ? "-x86_64" : string.Empty;
        return $"/Applications/Unity/Hub/Editor/{unityVersion}{x86_64}/Unity.app/Contents/MacOS/Unity";
    }
}
