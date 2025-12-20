using System;

namespace BiUM.Test.Infrastructure.Persistence.DbContextInitialiser;

public partial class TestDbContextInitialiser
{
    internal static class CurrencyIds
    {
        internal static Guid TryId = Guid.Parse("154aa9cc-3471-4ebe-bdc2-f1c6dd8c0f7c");
        internal static Guid UsdId = Guid.Parse("19a37168-4dd7-4cc4-bc76-6776e5c942a2");
        internal static Guid EurId = Guid.Parse("aba24392-f26a-403f-a311-42adb3d0b2c2");
        internal static Guid XauId = Guid.Parse("2a7ae36a-0e3b-4ca7-9ee7-65471d5d554e");
        internal static Guid XagId = Guid.Parse("07d7157e-7e14-4b42-ad21-f97282ddc395");
        internal static Guid BtcId = Guid.Parse("c351cab5-02ec-49f8-b93a-aa3e5773b32b");
    }
}