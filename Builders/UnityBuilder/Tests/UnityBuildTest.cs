namespace UnityBuilder.Tests;

public class UnityBuildTest
{
    public static void TestUnityBuild()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var projectPath = Path.Combine(home, "ci-cache", "Unity Test");

        var unityRunner = new UnityBuild2(
            projectPath,
            "Windows",
            ".exe",
            "Unity Test",
            "WindowsStandalone64",
            "Standalone",
            "Player",
            null!,
            null!,
            null!,
            0
        );

        unityRunner.Run();
    }
}
