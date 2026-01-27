using BiUM.Core.Common.Utils;
using System;

namespace BiUM.Core.Constants;

public static partial class Ids
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

            public static Guid BiRepairId = GuidGenerator.NewGuid("Application-BiRepair");
            public static Guid BiTicketId = GuidGenerator.NewGuid("Application-BiTicket");
        }

        public static class BiDynamicPortal
        {
            public static Guid Id = GuidGenerator.NewGuid("Application-BiDynamicPortal");
        }

        public static class BiDynamicBazaar
        {
            public static Guid Id = GuidGenerator.NewGuid("Application-BiDynamicBazaar");
        }

        public static class Bolt
        {
            public static Guid Id = GuidGenerator.NewGuid("Application-Bolt");
        }
    }
}
