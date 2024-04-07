namespace SocketServer;

public static class Print
{
    private const int KB = 1024;
    private const int MB = KB * KB;
    private const int GB = MB * KB;
    private const string DEFAULT_FORMAT = "0.00";
    
    /// <summary>
    /// Formats size to a MB or GB format
    /// </summary>
    /// <param name="size"></param>
    /// <param name="format"></param>
    /// <returns></returns>
    public static string ToByteSizeString(int size, string format = DEFAULT_FORMAT)
    {
        if (size >= GB)
            return $"{(size / (double)GB).ToString(format)} {nameof(GB)}";
        if (size >= MB)
            return $"{(size / (double)MB).ToString(format)} {nameof(MB)}";
        if (size >= KB)
            return $"{(size / (double)KB).ToString(format)} {nameof(KB)}";
        return $"{size} B";
    }
}