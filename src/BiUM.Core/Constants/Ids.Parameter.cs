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

        public static class DynamicExportRequestStatus
        {
            public static Guid Id = Guid.Parse("01a05545-57fd-7c15-a905-1139f5df44b2");

            public static class Values
            {
                public static Guid Pending = Guid.Parse("01a05545-60cd-7209-85d9-1d39b2989781");
                public static Guid Processing = Guid.Parse("01a05545-5ed4-77ef-87df-1e2b4d804d89");
                public static Guid Ready = Guid.Parse("01a05545-610f-7352-b235-bf328c90edaa");
                public static Guid Failed = Guid.Parse("01a05545-6160-791c-a09c-6f392fe37253");
                public static Guid Expired = Guid.Parse("01a05545-6164-7dfa-b139-4f9fb96f6141");
            }
        }

        public static class DynamicApiCompileStatus
        {
            public static Guid Id = Guid.Parse("01a05cf8-e803-7891-b88c-0fcab4f5329c");

            public static class Values
            {
                public static Guid Draft = Guid.Parse("01a05cf8-e992-7a70-bfee-c7a63f094136");
                public static Guid Success = Guid.Parse("01a05cf8-e980-717d-8287-f2fe1bfdfec7");
                public static Guid Failed = Guid.Parse("01a05cf8-e99b-7aec-aa86-525d4e00742a");
            }
        }

        public static class DynamicApiExecutionType
        {
            public static Guid Id = Guid.Parse("01a05cfa-56fb-707e-bac5-97833e42e2c3");

            public static class Values
            {
                public static Guid CSharpEf = Guid.Parse("01a05cfb-3080-7358-be4c-f197c68c2ed6");
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

        public static class ServiceParameterDirectionType
        {
            public static Guid Id = Guid.Parse("80497b41-35ff-5518-b3cc-72e7358ad06d");

            public static class Values
            {
                public static Guid In = Guid.Parse("a1bf7312-050c-5140-9fa5-30f27285915f");
                public static Guid Out = Guid.Parse("e8519f76-e7ec-54fb-aeed-c8d1cec2f915");
            }
        }

        public static class EventChannelType
        {
            public static Guid Id = Guid.Parse("01a09750-6218-76b0-a12c-e17dd174eaf9");

            public static class Values
            {
                public static Guid InternalRabbitMq = Guid.Parse("01a09750-6914-7a07-a4d9-6008671b680e");
                public static Guid ExternalRabbitMq = Guid.Parse("01a09750-67fc-7ac0-8109-3d14d3fba54a");
                public static Guid ExternalHttpWebhook = Guid.Parse("01a09750-693e-774a-9666-a191d6c5b400");
                public static Guid Mqtt = Guid.Parse("01a09750-6978-72cc-be4d-ae9b12f22a83");
            }
        }

        public static class EventDirectionType
        {
            public static Guid Id = Guid.Parse("01a09752-bee1-79a9-9eae-a45dfa28211b");

            public static class Values
            {
                public static Guid Inbound = Guid.Parse("01a09752-bf04-7117-a613-591df07a2087");
                public static Guid Outbound = Guid.Parse("01a09752-bf1f-7d0d-9b0f-cbd7cb33608e");
                public static Guid Bidirectional = Guid.Parse("01a09752-bf25-74a1-a81e-2c3000d980d5");
            }
        }

        public static class EventCredentialType
        {
            public static Guid Id = Guid.Parse("01a09755-515f-71e4-9e33-8407f292a20e");

            public static class Values
            {
                public static Guid ExternalRabbitMq = Guid.Parse("01a09755-51cf-7f95-b560-14cb8799e3fe");
                public static Guid Mqtt = Guid.Parse("01a09755-51d8-77c5-a452-8c95ec2f6c87");
                public static Guid WebhookHmac = Guid.Parse("01a09755-51ec-711a-87c8-23f2e3631216");
                public static Guid WebhookApiKey = Guid.Parse("01a09755-5211-708b-aa0d-6fa2ef6c3f3b");
            }
        }

        public static class EventActionType
        {
            public static Guid Id = Guid.Parse("01a09757-4f7b-7d17-b714-2cc5c1d4c3a4");

            public static class Values
            {
                public static Guid Service = Guid.Parse("01a09757-4fe8-734e-adc6-11acd7f126f6");
                public static Guid PublishEvent = Guid.Parse("01a09757-4fed-739d-b232-912b75532774");
                public static Guid InvokeEvent = Guid.Parse("01a09757-4ff3-7dc4-87e4-2903980c6456");
            }
        }

        public static class SchedulerTriggerType
        {
            public static Guid Id = Guid.Parse("01a09759-4f73-73e3-8235-192352cafb60");

            public static class Values
            {
                public static Guid Service = Guid.Parse("01a09759-500f-7baf-b87f-6bd2ee4bc5ae");
                public static Guid PublishEvent = Guid.Parse("01a09759-5017-7d6c-8d25-c3bd223311eb");
                public static Guid InvokeEvent = Guid.Parse("01a09759-502e-7889-bd22-04ff7f11a25d");
            }
        }

        public static class EventIntegrationStatusType
        {
            public static Guid Id = Guid.Parse("01a0975d-079c-716e-9d3d-db978229cf43");

            public static class Values
            {
                public static Guid Success = Guid.Parse("01a0975d-0826-7091-ba3b-ef7ea0914035");
                public static Guid Failed = Guid.Parse("01a0975d-0831-7ccc-a2d5-4d13c1cbfb10");
                public static Guid Skipped = Guid.Parse("01a0975d-0853-7767-b418-5024af8e74d6");
                public static Guid Timeout = Guid.Parse("01a0975d-0858-7a20-a353-fd6082c95019");
            }
        }
    }
}