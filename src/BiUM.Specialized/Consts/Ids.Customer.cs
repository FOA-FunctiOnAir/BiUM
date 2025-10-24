using BiUM.Specialized.Common.Utils;

namespace BiUM.Specialized.Consts;

public partial class Ids
{
    public static class Customer
    {
        public static class EmptyCustomer
        {
            internal static string Identity = "11111111111";
        }

        public static class System
        {
            internal static Guid Id = GuidGenerator.NewGuid("Customer-System");
            internal static Guid BranchId = GuidGenerator.NewGuid("Customer-Branch-System");
        }

        public static class Public
        {
            internal static Guid Id = GuidGenerator.NewGuid("Customer-Public");
            internal static Guid PersonId = GuidGenerator.NewGuid("Person-Public");
        }
    }
}