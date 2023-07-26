namespace SharedLib;

/// <summary>
/// Src: https://docs.unity3d.com/Manual/EditorCommandLineArguments.html Build Arguments
/// </summary>
public enum UnityTarget
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