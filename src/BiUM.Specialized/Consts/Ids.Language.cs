using BiUM.Core.Common.Utils;
using System;

namespace BiUM.Specialized.Consts;

public partial class Ids
{
    public static class Language
    {
        public static class Turkish
        {
            public static Guid Id = GuidGenerator.NewGuid("Language-Turkish");
        }

        public static class English
        {
            public static Guid Id = GuidGenerator.NewGuid("Language-English");
        }

        public static class Arabic
        {
            public static Guid Id = GuidGenerator.NewGuid("Language-Arabic");
        }
    }
}
