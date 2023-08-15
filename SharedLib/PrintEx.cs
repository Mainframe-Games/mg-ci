namespace SharedLib;

public static class PrintEx
{
	private const double MB = 1000000;
	private const double GB = 1073741824d;
	private const string DEFAULT_FORMAT = "0.0";

	public static string ToByteSizeString(this byte[] bytes, string format = DEFAULT_FORMAT)
	{
		return ((ulong)bytes.Length).ToByteSizeString(format);
	}
	
	public static string ToByteSizeString(this double size, string format = DEFAULT_FORMAT)
	{
		return ((ulong)size).ToByteSizeString(format);
	}

	/// <summary>
	/// Formats size to a MB or GB format
	/// </summary>
	/// <param name="size"></param>
	/// <param name="format"></param>
	/// <returns></returns>
	public static string ToByteSizeString(this ulong size, string format = DEFAULT_FORMAT)
	{
		return size < GB
			? $"{(size / MB).ToString(format)} {nameof(MB)}"
			: $"{(size / GB).ToString(format)} {nameof(GB)}";
	}
	
	public static string ToByteSizeString(this FilePacker.Entry[] frags)
	{
		ulong size = 0;

		foreach (var f in frags)
			size += f.SizeOf;
		
		return size.ToByteSizeString();
	}
}