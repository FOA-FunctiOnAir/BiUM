using BiUM.Contract.Models;

namespace BiUM.Tests.Helpers;

public static class CorrelationTestHelper
{
    public static CorrelationContext CreateBpmnLike(
        Guid tenantId,
        Guid applicationId,
        Guid? languageId = null,
        Guid? compensationSessionId = null)
    {
        var correlationId = Guid.NewGuid();

        return new CorrelationContext
        {
            CorrelationId = correlationId,
            TenantId = tenantId,
            ApplicationId = applicationId,
            LanguageId = languageId ?? CorrelationContext.DefaultLanguageId,
            CompensationSessionId = compensationSessionId,
            TraceId = "bpmn-test-trace",
            ConnectionId = "bpmn-signalr",
            ClientHost = "bpmn-engine"
        };
    }
}