using BiUM.Core.Common.Utils;
using System;

namespace BiUM.Core.Constants;

public partial class Ids
{
    public static class Parameter
    {
        public static class BpmnStatus
        {
            public static Guid Id = GuidGenerator.NewGuid("Parameter-BpmnStatusType");

            public static class Values
            {
                public static Guid New = GuidGenerator.NewGuid("ParameterValue-BpmnStatusType-New");
                public static Guid Active = GuidGenerator.NewGuid("ParameterValue-BpmnStatusType-Active");
                public static Guid Suspended = GuidGenerator.NewGuid("ParameterValue-BpmnStatusType-Suspended");
                public static Guid SubFlow = GuidGenerator.NewGuid("ParameterValue-BpmnStatusType-SubFlow");
                public static Guid Completed = GuidGenerator.NewGuid("ParameterValue-BpmnStatusType-Completed");
            }
        }

        public static class HttpType
        {
            public static Guid Id = GuidGenerator.NewGuid("Parameter-HttpType");

            public static class Values
            {
                public static Guid Get = GuidGenerator.NewGuid("ParameterValue-HttpType-Get");
                public static Guid Post = GuidGenerator.NewGuid("ParameterValue-HttpType-Post");
                public static Guid Put = GuidGenerator.NewGuid("ParameterValue-HttpType-Put");
                public static Guid Patch = GuidGenerator.NewGuid("ParameterValue-HttpType-Patch");
                public static Guid Delete = GuidGenerator.NewGuid("ParameterValue-HttpType-Delete");
                public static Guid Head = GuidGenerator.NewGuid("ParameterValue-HttpType-Head");
                public static Guid Options = GuidGenerator.NewGuid("ParameterValue-HttpType-Options");
            }
        }

        public static class ServiceType
        {
            public static Guid Id = Guid.Parse("875fa424-5b0f-48f1-8a98-a98f019e334d");

            public static class Values
            {
                public static Guid Internal = Guid.Parse("611ca025-0054-449a-a07e-6744db17bdc2");
                public static Guid DynamicApi = Guid.Parse("4f313134-001b-4b4d-8ed5-d98b9d96a349");
                public static Guid Crud = Guid.Parse("a0b2b902-7a07-4b0e-82c2-e68d0bb1abb0");
                public static Guid External = Guid.Parse("8259d88a-6b4e-4746-9567-eba60f74b7c3");
            }
        }

        public static class ServiceAuthType
        {
            public static Guid Id = Guid.Parse("e23d3ee6-8f81-4b67-b368-a87b2fe69d52");

            public static class Values
            {
                public static Guid NoAuth = Guid.Parse("f41c8795-bd4e-45b2-bbe7-dbeadce8a72a");
                public static Guid Basic = Guid.Parse("788b6a77-6504-455f-8177-4c6e6b0e84ed");
                public static Guid BearerStatic = Guid.Parse("ca8257c0-e2ca-41d5-a293-30d5b0ee5977");
                public static Guid ApiKeyHeader = Guid.Parse("e67806f2-18b5-40aa-9b7b-b0edb0d36a0f");
                public static Guid ApiKeyQuery = Guid.Parse("ce61f0d5-ce77-4050-a09c-3cf7f9869369");
                public static Guid OAuth2ClientCredentials = Guid.Parse("1a606032-c99f-4ca8-854a-65873e7891e9");
                public static Guid OAuth2Password = Guid.Parse("20943dd4-ae89-4464-8cce-39dce066cc02");
                public static Guid CustomHeader = Guid.Parse("29232346-baa2-4f06-9592-923fe3b706b7");
            }
        }
    }
}
