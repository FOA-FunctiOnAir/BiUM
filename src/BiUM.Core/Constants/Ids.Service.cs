using System;

namespace BiUM.Core.Constants;

public partial class Ids
{
    public static class Service
    {
        public static class SaveCrudServices
        {
            public static Guid Id = Guid.Parse("0ca6d366-29c3-480c-95d6-24b120341c98");
        }

        public static class SaveDynamicApiServices
        {
            public static Guid Id = Guid.Parse("01a05cf2-c627-7994-a7d2-31ab0d3993d8");
        }

        public static class PreviewInternalService
        {
            public static Guid Id = Guid.Parse("01a162ee-4335-722a-b9a4-b924ec99ef36");
        }

        public static class ApplyInternalService
        {
            public static Guid Id = Guid.Parse("01a162ee-4340-717b-b44d-64494c7d7828");
        }

        public static class GetServiceCatalogStatuses
        {
            public static Guid Id = Guid.Parse("01a1724d-99c1-7a45-b8ed-06e379e910d9");
        }
    }
}