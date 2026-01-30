namespace System;

public static partial class Extensions
{
    public static double ToDouble(this decimal source)
    {
        return Convert.ToDouble(source);
    }

    public static double? ToDouble(this decimal? source)
    {
        return source?.ToDouble();
    }

    public static decimal Round(this decimal source, int precision)
    {
        return Math.Round(source, precision, MidpointRounding.AwayFromZero);
    }

    public static decimal ToPercentage(this decimal percentage)
    {
        return percentage * 0.01m;
    }

    public static decimal ToDecimal(this double source)
    {
        return Convert.ToDecimal(source);
    }

    public static decimal? ToDecimal(this double? source)
    {
        return source?.ToDecimal();
    }
}