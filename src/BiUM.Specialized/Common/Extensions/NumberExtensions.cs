namespace System;

public static partial class Extensions
{
    public static decimal ToPercentage(this short percentage)
    {
        var value = percentage * 0.01m;

        return value;
    }

    public static decimal ToPercentage(this int percentage)
    {
        var value = percentage * 0.01m;

        return value;
    }
}