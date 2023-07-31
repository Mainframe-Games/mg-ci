namespace SharedLib;

/// <summary>
/// Src: https://docs.unity3d.com/Manual/EditorCommandLineArguments.html Build Arguments
/// </summary>
public enum BuildTargetFlag
{
	None,
	Standalone,
	Win,
	Win64,
	OSXUniversal,
	Linux64,
	iOS,
	Android,
	WebGL,
	WindowsStoreApps,
	tvOS
}

public enum UnitySubTarget
{
	Player,
	Server
}

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
		false)]
	CloudRendering = 41, // 0x00000029

	/// <summary>
	///   <para>Build a LinuxHeadlessSimulation standalone.</para>
	/// </summary>
	LinuxHeadlessSimulation = 41, // 0x00000029

	[Obsolete("GameCoreScarlett is deprecated, please use GameCoreXboxSeries (UnityUpgradable) -> GameCoreXboxSeries",
		false)]
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

public enum UnityBuildTargetGroup
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
	[Obsolete("Use WSA instead")] Metro = 14, // 0x0000000E

	/// <summary>
	///   <para>Windows Store Apps target.</para>
	/// </summary>
	WSA = 14, // 0x0000000E
	[Obsolete("Use WSA instead")] WP8 = 15, // 0x0000000F

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
		false)]
	CloudRendering = 30, // 0x0000001E

	/// <summary>
	///   <para>LinuxHeadlessSimulation target.</para>
	/// </summary>
	LinuxHeadlessSimulation = 30, // 0x0000001E

	[Obsolete("GameCoreScarlett is deprecated, please use GameCoreXboxSeries (UnityUpgradable) -> GameCoreXboxSeries",
		false)]
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