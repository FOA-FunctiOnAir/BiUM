using System.Text.Json;
using System.Globalization;

namespace System;

public static class JsonElementExtensions
{
    public static Dictionary<string, object?> ToDictionary(this JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException("JsonElement is not an object");

        var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (var prop in element.EnumerateObject())
        {
            dict[prop.Name] = ToNetObject(prop.Value);
        }

        return dict;
    }

    private static object? ToNetObject(JsonElement e)
    {
        switch (e.ValueKind)
        {
            case JsonValueKind.String:
                return e.GetString();

            case JsonValueKind.Number:
                if (e.TryGetInt64(out var i64)) return i64;
                if (e.TryGetDouble(out var dbl)) return dbl;
                return decimal.Parse(e.GetRawText(), CultureInfo.InvariantCulture);

            case JsonValueKind.True: return true;
            case JsonValueKind.False: return false;
            case JsonValueKind.Null: return null;

            case JsonValueKind.Object:
                return e.ToDictionary();

            case JsonValueKind.Array:
                {
                    var list = new List<object?>();
                    foreach (var item in e.EnumerateArray())
                        list.Add(ToNetObject(item));
                    return list;
                }

            default:
                return e.GetRawText();
        }
    }
}