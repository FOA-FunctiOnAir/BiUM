namespace System;

public static partial class Extensions
{
    public static double ToDouble(this decimal source)
    {
        var value = Convert.ToDouble(source);

        return value;
    }

    public static double? ToDouble(this decimal? source)
    {
        if (source is null)
        {
            return null;
        }

        var value = source.Value.ToDouble();

        return value;
    }

    public static decimal Round(this decimal source, int precision)
    {
        var value = Math.Round(source, precision, MidpointRounding.AwayFromZero);

        return value;
    }

    public static decimal ToPercentage(this decimal percentage)
    {
        var value = percentage * 0.01m;

        return value;
    }

    public static decimal ToDecimal(this double source)
    {
        var value = Convert.ToDecimal(source);

        return value;
    }

    public static decimal? ToDecimal(this double? source)
    {
        if (source is null)
        {
            return null;
        }

        var value = Convert.ToDecimal(source);

        return value;
    }
}
