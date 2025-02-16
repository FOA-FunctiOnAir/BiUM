using BiUM.Specialized.Common.Utils;

namespace BiUM.Specialized.Consts;

public partial class Ids
{
    public static class Application
    {
        public static class System
        {
            public static Guid Id = GuidGenerator.NewGuid("Application-System");
        }

        public static class BiDynamic
        {
            public static Guid Id = GuidGenerator.NewGuid("Application-BiDynamic");

            public static Guid BiServiceId = GuidGenerator.NewGuid("Application-BiService");
            public static Guid BiTicketId = GuidGenerator.NewGuid("Application-BiTicket");
        }

        public static class Bolt
        {
            public static Guid Id = GuidGenerator.NewGuid("Application-Bolt");
        }

        public static class BiDynamicSite
        {
            public static Guid Id = GuidGenerator.NewGuid("Application-BiDynamicSite");
        }
    }
}