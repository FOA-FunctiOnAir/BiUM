﻿using BiUM.Specialized.Common.Utils;

namespace BiUM.Specialized.Consts;

public partial class Ids
{
    public static class Customer
    {
        public static class EmptyCustomer
        {
            public static string Identity = "11111111111";
        }

        public static class System
        {
            public static Guid Id = GuidGenerator.NewGuid("Customer-System");
            public static Guid BranchId = GuidGenerator.NewGuid("Customer-Branch-System");
        }

        public static class Public
        {
            public static Guid Id = GuidGenerator.NewGuid("Customer-Public");
            public static Guid PersonId = GuidGenerator.NewGuid("Person-Public");
        }
    }
}