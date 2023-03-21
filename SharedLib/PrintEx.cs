namespace SharedLib;

public static class PrintEx
{
	private const uint MB = 1000000;

	public static string ToMegaByteString(this byte[] bytes)
	{
		return $"{bytes.Length / MB} {nameof(MB)}";
	}
}