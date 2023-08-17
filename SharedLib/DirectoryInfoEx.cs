namespace SharedLib;

public static class DirectoryInfoEx
{
	public static ulong GetByteSize(this DirectoryInfo dir)
	{
		ulong size = 0;
		
		// Add file sizes.
		foreach (var fi in dir.GetFiles())
			size += (ulong)fi.Length;
		
		// Add subdirectory sizes.
		foreach (var di in dir.GetDirectories())
			size += di.GetByteSize();

		return size;
	}
}