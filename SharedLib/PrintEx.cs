namespace SharedLib;

public static class PrintEx
{
	private const double MB = 1000000;
	private const double GB = 1073741824d;

	public static string ToMegaByteString(this byte[] bytes, string format = "0")
	{
		return $"{(bytes.Length / MB).ToString(format)} {nameof(MB)}";
	}
	
	public static string ToGigaByteString(this double size, string format = "0")
	{
		return $"{(size / GB).ToString(format)} {nameof(GB)}";
	}
}