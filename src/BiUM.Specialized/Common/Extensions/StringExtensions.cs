namespace System.Linq;

public static partial class Extensions
{
    public static Guid? ToGuidList(this string source)
    {
        if (Guid.TryParse(source, out var guid))
        {
            return guid;
        }

        return null;
    }
}