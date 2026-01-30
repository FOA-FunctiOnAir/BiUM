using BiUM.Core.Common.Utils;
using System;

namespace BiUM.Core.Constants;

public partial class Ids
{
    public static class Channel
    {
        public static class Web
        {
            public static Guid Id = GuidGenerator.NewGuid("Channel-Web");
        }

        public static class Ios
        {
            public static Guid Id = GuidGenerator.NewGuid("Channel-Ios");
        }

        public static class Android
        {
            public static Guid Id = GuidGenerator.NewGuid("Channel-Android");
        }
    }
}