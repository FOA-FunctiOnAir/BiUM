using BiUM.Specialized.Common.Utils;

namespace BiUM.Specialized.Consts;

public partial class Ids
{
    public static class Application
    {
        public static class BiDynamic
        {
            public static Guid Id = GuidGenerator.NewGuid("Application-BiDynamic");
        }

        public static class BiDynamicSite
        {
            public static Guid Id = GuidGenerator.NewGuid("Application-BiDynamicSite");
        }
    }
}