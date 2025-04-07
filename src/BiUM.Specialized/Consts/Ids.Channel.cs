using BiUM.Specialized.Common.Utils;

namespace BiUM.Specialized.Consts;

public partial class Ids
{
    public static class Channel
    {
        public static class Web
        {
            internal static Guid Id = GuidGenerator.NewGuid("Channel-Web");
        }

        public static class Ios
        {
            internal static Guid Id = GuidGenerator.NewGuid("Channel-Ios");
        }

        public static class Android
        {
            internal static Guid Id = GuidGenerator.NewGuid("Channel-Android");
        }
    }
}