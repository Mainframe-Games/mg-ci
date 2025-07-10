using System;

namespace SharedLib;

public static class PrintEx
{
    private const double MB = 1000000;
    private const double GB = 1073741824d;
    private const string DEFAULT_FORMAT = "0.0";

    public static string ToHourMinSecString(this TimeSpan timeSpan)
    {
        return timeSpan.ToString(@"hh\:mm\:ss");
    }
}
