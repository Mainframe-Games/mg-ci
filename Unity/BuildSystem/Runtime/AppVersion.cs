using System;
using System.IO;
using System.Text;

namespace BuildSystem
{
	public readonly struct AppVersion : IEquatable<AppVersion>, IComparable<AppVersion>, IComparable
	{
		private const string FILE_NAME = "build_version.txt";

		/// <summary>
		/// Only want to compare Maj, Min, Patch
		/// </summary>
		private const int LENGTH = 3;

		public uint? Major { get; }
		public uint? Minor { get; }
		public uint? Patch { get; }
		public uint? Build { get; }

		/// <summary>
		/// Returns `{prefix}{major}.{minor}.{patch}.{build}`
		/// </summary>
		public string DisplayString { get; }

		/// <summary>
		/// Returns raw `{major}.{minor}.{patch}.{build}`
		/// </summary>
		public string RawString { get; }

		private uint this[int i]
		{
			get
			{
				return i switch
				{
					0 => Major ?? 0,
					1 => Minor ?? 0,
					2 => Patch ?? 0,
					3 => Build ?? 0,
					_ => 0
				};
			}
		}

		public AppVersion(uint? major, uint? minor = null, uint? patch = null, uint? build = null, string displayPrefix = null)
		{
			Major = major;
			Minor = minor;
			Patch = patch;
			Build = build;

			RawString = GetVersionString(Major, Minor, Patch, Build);
			DisplayString = $"{displayPrefix}{RawString}";
		}

		public AppVersion(string versionString, string displayPrefix = null)
		{
			var split = versionString?.Split('.') ?? Array.Empty<string>();

			Major = null;
			Minor = null;
			Patch = null;
			Build = null;

			for (int i = 0; i < split.Length; i++)
			{
				if (!uint.TryParse(split[i], out var num))
					continue;

				switch (i)
				{
					case 0:
						Major = num;
						break;
					case 1:
						Minor = num;
						break;
					case 2:
						Patch = num;
						break;
					case 3:
						Build = num;
						break;
					default: break;
				}
			}

			RawString = GetVersionString(Major, Minor, Patch, Build);
			DisplayString = $"{displayPrefix}{RawString}";
		}

		/// <summary>
		/// Returns version struct from file at root of project <see cref="FILE_NAME"/>
		/// </summary>
		/// <param name="displayString"></param>
		/// <returns></returns>
		public static AppVersion GetFromFile(string displayString)
		{
			if (!File.Exists(FILE_NAME))
				return default;

			var ver = File.ReadAllText(FILE_NAME);
			return new AppVersion(ver, displayString);
		}

		private static string GetVersionString(params uint?[] vers)
		{
			var str = new StringBuilder();

			foreach (var ver in vers)
				if (ver is not null)
					str.Append($"{ver}.");

			return str.ToString().Trim('.');
		}

		public override string ToString()
		{
			return RawString;
		}

		#region Interface Implementations

		public override int GetHashCode()
		{
			return HashCode.Combine(Major, Minor, Patch, Build);
		}

		public int CompareTo(object obj)
		{
			return CompareTo((AppVersion)obj);
		}

		public int CompareTo(AppVersion other)
		{
			if (Major is not null)
			{
				var val = Major?.CompareTo(other.Major) ?? 0;
				if (val != 0) return val;
			}

			if (Minor is not null)
			{
				var val = Minor?.CompareTo(other.Minor) ?? 0;
				if (val != 0) return val;
			}

			if (Patch is not null)
			{
				var val = Patch?.CompareTo(other.Patch) ?? 0;
				if (val != 0) return val;
			}

			return 0;
		}

		public bool Equals(AppVersion other)
		{
			return Major == other.Major
			       && Minor == other.Minor
			       && Patch == other.Patch;
		}

		public override bool Equals(object obj)
		{
			return obj is AppVersion other && Equals(other);
		}

		public static bool operator ==(AppVersion left, AppVersion right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(AppVersion left, AppVersion right)
		{
			return !left.Equals(right);
		}

		public static bool operator <(AppVersion left, AppVersion right)
		{
			for (int i = 0; i < LENGTH; i++)
			{
				if (left[i] == right[i])
					continue;

				return left[i] < right[i];
			}

			return false;
		}

		public static bool operator >(AppVersion left, AppVersion right)
		{
			for (int i = 0; i < LENGTH; i++)
			{
				if (left[i] == right[i])
					continue;

				return left[i] > right[i];
			}

			return false;
		}

		public static bool operator <=(AppVersion left, AppVersion right)
		{
			return left < right || left == right;
		}

		public static bool operator >=(AppVersion left, AppVersion right)
		{
			return left > right || left == right;
		}

		#endregion
	}
}