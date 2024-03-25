using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.Serialization;
using AvaloniaAppMVVM.ViewModels;

namespace AvaloniaAppMVVM.Data;

public class Unity
{
    public enum BuildTarget
    {
        NoTarget = -2, // 0xFFFFFFFE

        [Obsolete("BlackBerry has been removed in 5.4")]
        BB10 = -1, // 0xFFFFFFFF

        [Obsolete("Use WSAPlayer instead (UnityUpgradable) -> WSAPlayer", true)]
        MetroPlayer = -1, // 0xFFFFFFFF

        /// <summary>
        ///   <para>OBSOLETE: Use iOS. Build an iOS player.</para>
        /// </summary>
        [Obsolete("Use iOS instead (UnityUpgradable) -> iOS", true)]
        iPhone = -1, // 0xFFFFFFFF

        /// <summary>
        ///   <para>Build a macOS standalone (Intel 64-bit).</para>
        /// </summary>
        StandaloneOSX = 2,

        [Obsolete("Use StandaloneOSX instead (UnityUpgradable) -> StandaloneOSX", true)]
        StandaloneOSXUniversal = 3,

        /// <summary>
        ///   <para>Build a macOS Intel 32-bit standalone. (This build target is deprecated)</para>
        /// </summary>
        [Obsolete("StandaloneOSXIntel has been removed in 2017.3")]
        StandaloneOSXIntel = 4,

        /// <summary>
        ///   <para>Build a Windows standalone.</para>
        /// </summary>
        StandaloneWindows = 5,

        /// <summary>
        ///   <para>Build a web player. (This build target is deprecated. Building for web player will no longer be supported in future versions of Unity.)</para>
        /// </summary>
        [Obsolete("WebPlayer has been removed in 5.4", true)]
        WebPlayer = 6,

        /// <summary>
        ///   <para>Build a streamed web player.</para>
        /// </summary>
        [Obsolete("WebPlayerStreamed has been removed in 5.4", true)]
        WebPlayerStreamed = 7,

        /// <summary>
        ///   <para>Build an iOS player.</para>
        /// </summary>
        iOS = 9,

        [Obsolete("PS3 has been removed in >=5.5")]
        PS3 = 10, // 0x0000000A

        [Obsolete("XBOX360 has been removed in 5.5")]
        XBOX360 = 11, // 0x0000000B

        /// <summary>
        ///   <para>Build an Android .apk standalone app.</para>
        /// </summary>
        Android = 13, // 0x0000000D

        /// <summary>
        ///   <para>Build a Linux standalone.</para>
        /// </summary>
        [Obsolete("StandaloneLinux has been removed in 2019.2")]
        StandaloneLinux = 17, // 0x00000011

        /// <summary>
        ///   <para>Build a Windows 64-bit standalone.</para>
        /// </summary>
        StandaloneWindows64 = 19, // 0x00000013

        /// <summary>
        ///   <para>Build to WebGL platform.</para>
        /// </summary>
        WebGL = 20, // 0x00000014

        /// <summary>
        ///   <para>Build an Windows Store Apps player.</para>
        /// </summary>
        WSAPlayer = 21, // 0x00000015

        /// <summary>
        ///   <para>Build a Linux 64-bit standalone.</para>
        /// </summary>
        StandaloneLinux64 = 24, // 0x00000018

        /// <summary>
        ///   <para>Build a Linux universal standalone.</para>
        /// </summary>
        [Obsolete("StandaloneLinuxUniversal has been removed in 2019.2")]
        StandaloneLinuxUniversal = 25, // 0x00000019

        [Obsolete("Use WSAPlayer with Windows Phone 8.1 selected")]
        WP8Player = 26, // 0x0000001A

        /// <summary>
        ///   <para>Build a macOS Intel 64-bit standalone. (This build target is deprecated)</para>
        /// </summary>
        [Obsolete("StandaloneOSXIntel64 has been removed in 2017.3")]
        StandaloneOSXIntel64 = 27, // 0x0000001B

        [Obsolete("BlackBerry has been removed in 5.4")]
        BlackBerry = 28, // 0x0000001C

        [Obsolete("Tizen has been removed in 2017.3")]
        Tizen = 29, // 0x0000001D

        [Obsolete("PSP2 is no longer supported as of Unity 2018.3")]
        PSP2 = 30, // 0x0000001E

        /// <summary>
        ///   <para>Build a PS4 Standalone.</para>
        /// </summary>
        PS4 = 31, // 0x0000001F

        [Obsolete("PSM has been removed in >= 5.3")]
        PSM = 32, // 0x00000020

        /// <summary>
        ///   <para>Build a Xbox One Standalone.</para>
        /// </summary>
        XboxOne = 33, // 0x00000021

        [Obsolete("SamsungTV has been removed in 2017.3")]
        SamsungTV = 34, // 0x00000022

        /// <summary>
        ///   <para>Build to Nintendo 3DS platform.</para>
        /// </summary>
        [Obsolete("Nintendo 3DS support is unavailable since 2018.1")]
        N3DS = 35, // 0x00000023

        [Obsolete("Wii U support was removed in 2018.1")]
        WiiU = 36, // 0x00000024

        /// <summary>
        ///   <para>Build to Apple's tvOS platform.</para>
        /// </summary>
        tvOS = 37, // 0x00000025

        /// <summary>
        ///   <para>Build a Nintendo Switch player.</para>
        /// </summary>
        Switch = 38, // 0x00000026

        [Obsolete("Lumin has been removed in 2022.2")]
        Lumin = 39, // 0x00000027

        /// <summary>
        ///   <para>Build a Stadia standalone.</para>
        /// </summary>
        Stadia = 40, // 0x00000028

        /// <summary>
        ///   <para>Build a CloudRendering standalone.</para>
        /// </summary>
        [Obsolete(
            "CloudRendering is deprecated, please use LinuxHeadlessSimulation (UnityUpgradable) -> LinuxHeadlessSimulation",
            false
        )]
        CloudRendering = 41, // 0x00000029

        /// <summary>
        ///   <para>Build a LinuxHeadlessSimulation standalone.</para>
        /// </summary>
        LinuxHeadlessSimulation = 41, // 0x00000029

        [Obsolete(
            "GameCoreScarlett is deprecated, please use GameCoreXboxSeries (UnityUpgradable) -> GameCoreXboxSeries",
            false
        )]
        GameCoreScarlett = 42, // 0x0000002A
        GameCoreXboxSeries = 42, // 0x0000002A
        GameCoreXboxOne = 43, // 0x0000002B

        /// <summary>
        ///   <para>Build to PlayStation 5 platform.</para>
        /// </summary>
        PS5 = 44, // 0x0000002C
        EmbeddedLinux = 45, // 0x0000002D
        QNX = 46, // 0x0000002E

        /// <summary>
        ///   <para>Build a visionOS player.</para>
        /// </summary>
        VisionOS = 47, // 0x0000002F
    }

    public enum BuildTargetGroup
    {
        /// <summary>
        ///   <para>Unknown target.</para>
        /// </summary>
        Unknown = 0,

        /// <summary>
        ///   <para>PC (Windows, Mac, Linux) target.</para>
        /// </summary>
        Standalone = 1,

        /// <summary>
        ///   <para>Mac/PC webplayer target.</para>
        /// </summary>
        [Obsolete("WebPlayer was removed in 5.4, consider using WebGL", true)]
        WebPlayer = 2,

        /// <summary>
        ///   <para>Apple iOS target.</para>
        /// </summary>
        iOS = 4,

        /// <summary>
        ///   <para>OBSOLETE: Use iOS. Apple iOS target.</para>
        /// </summary>
        [Obsolete("Use iOS instead (UnityUpgradable) -> iOS", true)]
        iPhone = 4,

        [Obsolete("PS3 has been removed in >=5.5")]
        PS3 = 5,

        [Obsolete("XBOX360 has been removed in 5.5")]
        XBOX360 = 6,

        /// <summary>
        ///   <para>Android target.</para>
        /// </summary>
        Android = 7,

        /// <summary>
        ///   <para>WebGL.</para>
        /// </summary>
        WebGL = 13, // 0x0000000D

        [Obsolete("Use WSA instead")]
        Metro = 14, // 0x0000000E

        /// <summary>
        ///   <para>Windows Store Apps target.</para>
        /// </summary>
        WSA = 14, // 0x0000000E

        [Obsolete("Use WSA instead")]
        WP8 = 15, // 0x0000000F

        [Obsolete("BlackBerry has been removed as of 5.4")]
        BlackBerry = 16, // 0x00000010

        [Obsolete("Tizen has been removed in 2017.3")]
        Tizen = 17, // 0x00000011

        [Obsolete("PSP2 is no longer supported as of Unity 2018.3")]
        PSP2 = 18, // 0x00000012

        /// <summary>
        ///   <para>Sony Playstation 4 target.</para>
        /// </summary>
        PS4 = 19, // 0x00000013

        [Obsolete("PSM has been removed in >= 5.3")]
        PSM = 20, // 0x00000014

        /// <summary>
        ///   <para>Microsoft Xbox One target.</para>
        /// </summary>
        XboxOne = 21, // 0x00000015

        [Obsolete("SamsungTV has been removed as of 2017.3")]
        SamsungTV = 22, // 0x00000016

        /// <summary>
        ///   <para>Nintendo 3DS target.</para>
        /// </summary>
        [Obsolete("Nintendo 3DS support is unavailable since 2018.1")]
        N3DS = 23, // 0x00000017

        [Obsolete("Wii U support was removed in 2018.1")]
        WiiU = 24, // 0x00000018

        /// <summary>
        ///   <para>Apple's tvOS target.</para>
        /// </summary>
        tvOS = 25, // 0x00000019

        [Obsolete("Facebook support was removed in 2019.3")]
        Facebook = 26, // 0x0000001A

        /// <summary>
        ///   <para>Nintendo Switch target.</para>
        /// </summary>
        Switch = 27, // 0x0000001B

        [Obsolete("Lumin has been removed in 2022.2")]
        Lumin = 28, // 0x0000001C

        /// <summary>
        ///   <para>Google Stadia target.</para>
        /// </summary>
        Stadia = 29, // 0x0000001D

        /// <summary>
        ///   <para>CloudRendering target.</para>
        /// </summary>
        [Obsolete(
            "CloudRendering is deprecated, please use LinuxHeadlessSimulation (UnityUpgradable) -> LinuxHeadlessSimulation",
            false
        )]
        CloudRendering = 30, // 0x0000001E

        /// <summary>
        ///   <para>LinuxHeadlessSimulation target.</para>
        /// </summary>
        LinuxHeadlessSimulation = 30, // 0x0000001E

        [Obsolete(
            "GameCoreScarlett is deprecated, please use GameCoreXboxSeries (UnityUpgradable) -> GameCoreXboxSeries",
            false
        )]
        GameCoreScarlett = 31, // 0x0000001F
        GameCoreXboxSeries = 31, // 0x0000001F
        GameCoreXboxOne = 32, // 0x00000020

        /// <summary>
        ///   <para>Sony Playstation 5 target.</para>
        /// </summary>
        PS5 = 33, // 0x00000021
        EmbeddedLinux = 34, // 0x00000022
        QNX = 35, // 0x00000023

        /// <summary>
        ///   <para>Apple visionOS target.</para>
        /// </summary>
        VisionOS = 36, // 0x00000024
    }

    public enum SubTarget
    {
        Player,
        Server,
        NoSubtarget
    }

    /// <summary>
    ///   <para>Building options. Multiple options can be combined together.</para>
    /// </summary>
    [Flags]
    public enum BuildOptions
    {
        /// <summary>
        ///   <para>Perform the specified build without any special settings or extra tasks.</para>
        /// </summary>
        None = 0,

        /// <summary>
        ///   <para>Build a development version of the player.</para>
        /// </summary>
        Development = 1,

        /// <summary>
        ///   <para>Run the built player.</para>
        /// </summary>
        AutoRunPlayer = 4,

        /// <summary>
        ///   <para>Show the built player.</para>
        /// </summary>
        ShowBuiltPlayer = 8,

        /// <summary>
        ///   <para>Build a compressed asset bundle that contains streamed Scenes loadable with the UnityWebRequest class.</para>
        /// </summary>
        BuildAdditionalStreamedScenes = 16, // 0x00000010

        /// <summary>
        ///   <para>Used when building Xcode (iOS) or Eclipse (Android) projects.</para>
        /// </summary>
        AcceptExternalModificationsToPlayer = 32, // 0x00000020
        InstallInBuildFolder = 64, // 0x00000040

        /// <summary>
        ///   <para>Clear all cached build results, resulting in a full rebuild of all scripts and all player data.</para>
        /// </summary>
        CleanBuildCache = 128, // 0x00000080

        /// <summary>
        ///   <para>Start the player with a connection to the profiler in the editor.</para>
        /// </summary>
        ConnectWithProfiler = 256, // 0x00000100

        /// <summary>
        ///   <para>Allow script debuggers to attach to the Player remotely. You can debug your scripts only if you use BuildOptions.Development.</para>
        /// </summary>
        AllowDebugging = 512, // 0x00000200

        /// <summary>
        ///   <para>Symlink runtime libraries when generating iOS Xcode project. (Faster iteration time).</para>
        /// </summary>
        [Obsolete(
            "BuildOptions.SymlinkLibraries is obsolete. Use BuildOptions.SymlinkSources instead (UnityUpgradable) -> [UnityEditor] BuildOptions.SymlinkSources",
            false
        )]
        SymlinkLibraries = 1024, // 0x00000400

        /// <summary>
        ///   <para>Symlink sources when generating the project. This is useful if you're changing source files inside the generated project and want to bring the changes back into your Unity project or a package.</para>
        /// </summary>
        SymlinkSources = SymlinkLibraries, // 0x00000400

        /// <summary>
        ///   <para>Don't compress the data when creating the asset bundle.</para>
        /// </summary>
        UncompressedAssetBundle = 2048, // 0x00000800

        [Obsolete("Use BuildOptions.Development instead")]
        StripDebugSymbols = 0,

        [Obsolete("Texture Compression is now always enabled")]
        CompressTextures = 0,

        /// <summary>
        ///   <para>Sets the Player to connect to the Editor.</para>
        /// </summary>
        ConnectToHost = 4096, // 0x00001000

        /// <summary>
        ///   <para>Determines if the player should be using the custom connection ID.</para>
        /// </summary>
        CustomConnectionID = 8192, // 0x00002000

        /// <summary>
        ///   <para>Options for building the standalone player in headless mode.</para>
        /// </summary>
        [Obsolete("Use StandaloneBuildSubtarget.Server instead")]
        EnableHeadlessMode = 16384, // 0x00004000

        /// <summary>
        ///   <para>Only build the scripts in a Project.</para>
        /// </summary>
        BuildScriptsOnly = 32768, // 0x00008000

        /// <summary>
        ///         <para>Patch a Development app package rather than completely rebuilding it.
        ///
        /// Supported platforms: Android.</para>
        ///       </summary>
        PatchPackage = 65536, // 0x00010000

        [
            EditorBrowsable(EditorBrowsableState.Never),
            Obsolete(
                "BuildOptions.IL2CPP is deprecated and has no effect. Use PlayerSettings.SetScriptingBackend() instead.",
                true
            )
        ]
        Il2CPP = 0,

        /// <summary>
        ///   <para>Include assertions in the build. By default, the assertions are only included in development builds.</para>
        /// </summary>
        ForceEnableAssertions = 131072, // 0x00020000

        /// <summary>
        ///   <para>Use chunk-based LZ4 compression when building the Player.</para>
        /// </summary>
        CompressWithLz4 = 262144, // 0x00040000

        /// <summary>
        ///   <para>Use chunk-based LZ4 high-compression when building the Player.</para>
        /// </summary>
        CompressWithLz4HC = 524288, // 0x00080000

        /// <summary>
        ///   <para>Force full optimizations for script compilation in Development builds.</para>
        /// </summary>
        [Obsolete("Specify IL2CPP optimization level in Player Settings.")]
        ForceOptimizeScriptCompilation = 0,
        ComputeCRC = 1048576, // 0x00100000

        /// <summary>
        ///   <para>Do not allow the build to succeed if any errors are reporting during it.</para>
        /// </summary>
        StrictMode = 2097152, // 0x00200000

        /// <summary>
        ///   <para>Build will include Assemblies for testing.</para>
        /// </summary>
        IncludeTestAssemblies = 4194304, // 0x00400000

        /// <summary>
        ///   <para>Will force the buildGUID to all zeros.</para>
        /// </summary>
        NoUniqueIdentifier = 8388608, // 0x00800000

        /// <summary>
        ///   <para>Sets the Player to wait for player connection on player start.</para>
        /// </summary>
        WaitForPlayerConnection = 33554432, // 0x02000000

        /// <summary>
        ///   <para>Enables code coverage. You can use this as a complimentary way of enabling code coverage on platforms that do not support command line arguments.</para>
        /// </summary>
        EnableCodeCoverage = 67108864, // 0x04000000

        /// <summary>
        ///   <para>Enables Deep Profiling support in the player.</para>
        /// </summary>
        EnableDeepProfilingSupport = 268435456, // 0x10000000

        /// <summary>
        ///   <para>Generates more information in the BuildReport.</para>
        /// </summary>
        DetailedBuildReport = 536870912, // 0x20000000

        /// <summary>
        ///   <para>Enable Shader Livelink support.</para>
        /// </summary>
        ShaderLivelinkSupport = 1073741824, // 0x40000000
    }
}

public class UnityBuildTarget : CiProcess
{
    public string? Name { get; set; } = "New Build Target";

    // config
    public string? Extension { get; set; } = ".exe";
    public string? ProductName { get; set; }
    public Unity.BuildTarget Target { get; set; } = Unity.BuildTarget.StandaloneWindows64;
    public Unity.BuildTargetGroup TargetGroup { get; set; } = Unity.BuildTargetGroup.Standalone;
    public Unity.SubTarget? SubTarget { get; set; } = Unity.SubTarget.Player;
    public string? BuildPath { get; set; }
    public List<string> Scenes { get; set; } = [];
    public List<string> ExtraScriptingDefines { get; set; } = [];
    public List<string> AssetBundleManifestPath { get; set; } = [];
    public int BuildOptions { get; set; } = (int)Unity.BuildOptions.None;

    [IgnoreDataMember]
    public ObservableCollection<string> ExtensionOptions { get; } =
        [".exe", ".app", ".x86_64", ".apk", "/"];

    [IgnoreDataMember]
    public ObservableCollection<string> BuildTargetOptions { get; } =
        new(Enum.GetNames(typeof(Unity.BuildTarget)));

    [IgnoreDataMember]
    public ObservableCollection<string> BuildTargetGroupOptions { get; } =
        new(Enum.GetNames(typeof(Unity.BuildTargetGroup)));

    [IgnoreDataMember]
    public ObservableCollection<string> SubTargetOptions { get; } =
        new(Enum.GetNames(typeof(Unity.SubTarget)));

    [IgnoreDataMember]
    public ObservableCollection<string> BuildOptionOptions { get; } =
        new(Enum.GetNames(typeof(Unity.BuildOptions)));
}
