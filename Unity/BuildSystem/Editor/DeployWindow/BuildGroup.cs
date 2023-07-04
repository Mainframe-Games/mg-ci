using System;
using UnityEditor;

namespace BuildSystem
{
	public struct BuildGroup : IEquatable<BuildGroup>
	{
		public BuildTargetGroup TargetGroup;
		public BuildTarget Target;
		public StandaloneBuildSubtarget SubTarget;

		private BuildGroup(BuildTargetGroup targetGroup, BuildTarget target, StandaloneBuildSubtarget subTarget)
		{
			TargetGroup = targetGroup;
			Target = target;
			SubTarget = subTarget;
		}

		public static BuildGroup Current => new(
			EditorUserBuildSettings.selectedBuildTargetGroup,
			EditorUserBuildSettings.activeBuildTarget,
			EditorUserBuildSettings.standaloneBuildSubtarget);
		
		public static BuildGroup DefaultPlatform =>
			Environment.OSVersion.Platform is PlatformID.MacOSX 
				? DefaultMac
				: DefaultWin;
		
		private static readonly BuildGroup DefaultWin = new(
			BuildTargetGroup.Standalone,
			BuildTarget.StandaloneWindows64,
			StandaloneBuildSubtarget.Player);
		
		private static readonly BuildGroup DefaultMac = new(
			BuildTargetGroup.Standalone,
			BuildTarget.StandaloneOSX,
			StandaloneBuildSubtarget.Player);

		public override string ToString()
		{
			return $"{TargetGroup}, {Target}, {SubTarget}";
		}

		public bool Equals(BuildGroup other)
		{
			return TargetGroup == other.TargetGroup && Target == other.Target && SubTarget == other.SubTarget;
		}

		public override bool Equals(object obj)
		{
			return obj is BuildGroup other && Equals(other);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine((int)TargetGroup, (int)Target, (int)SubTarget);
		}
	}
}