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
            LanguageId = languageId ?? Guid.Parse("11111111-1111-1111-1111-111111111111"),
            CompensationSessionId = compensationSessionId,
            TraceId = "bpmn-test-trace",
            ConnectionId = "bpmn-signalr",
            ClientHost = "bpmn-engine"
        };
    }
}