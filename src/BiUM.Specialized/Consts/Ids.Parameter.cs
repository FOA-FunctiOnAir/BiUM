using BiUM.Specialized.Common.Utils;

namespace BiUM.Specialized.Consts;

public partial class Ids
{
    public static class Parameter
    {
        public static class BpmnStatus
        {
            public static Guid Id = GuidGenerator.NewGuid("Parameter-BpmnStatusType");

            public static class Values
            {
                internal static Guid New = GuidGenerator.NewGuid("ParameterValue-BpmnStatusType-New");
                internal static Guid Active = GuidGenerator.NewGuid("ParameterValue-BpmnStatusType-Active");
                internal static Guid Suspended = GuidGenerator.NewGuid("ParameterValue-BpmnStatusType-Suspended");
                internal static Guid SubFlow = GuidGenerator.NewGuid("ParameterValue-BpmnStatusType-SubFlow");
                internal static Guid Completed = GuidGenerator.NewGuid("ParameterValue-BpmnStatusType-Completed");
            }
        }

        public static class HttpType
        {
            public static Guid Id = GuidGenerator.NewGuid("Parameter-HttpType");

            internal static class Values
            {
                internal static Guid Get = GuidGenerator.NewGuid("ParameterValue-HttpType-Get");
                internal static Guid Post = GuidGenerator.NewGuid("ParameterValue-HttpType-Post");
                internal static Guid Put = GuidGenerator.NewGuid("ParameterValue-HttpType-Put");
                internal static Guid Patch = GuidGenerator.NewGuid("ParameterValue-HttpType-Patch");
                internal static Guid Delete = GuidGenerator.NewGuid("ParameterValue-HttpType-Delete");
                internal static Guid Head = GuidGenerator.NewGuid("ParameterValue-HttpType-Head");
                internal static Guid Options = GuidGenerator.NewGuid("ParameterValue-HttpType-Options");
            }
        }
    }
}