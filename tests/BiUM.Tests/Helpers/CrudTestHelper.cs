using BiUM.Contract.Enums;
using BiUM.Contract.Models;
using BiUM.Contract.Models.Api;
using BiUM.Core.Constants;
using BiUM.Infrastructure.Common.Models;
using BiUM.Specialized.Database;
using BiUM.Specialized.Services;
using Moq;

namespace BiUM.Tests.Helpers;

public static class CrudTestHelper
{
    public static void WireTranslationMockToEchoCodes(Mock<ITranslationService> translationMock)
    {
        translationMock
            .Setup(t => t.AddMessage(
                It.IsAny<ApiResponse>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Callback<ApiResponse, string, CancellationToken>((response, code, _) =>
                response.AddMessage(new ResponseMessage { Code = code, Message = code, Severity = MessageSeverity.Error }));

        translationMock
            .Setup(t => t.AddMessage(
                It.IsAny<ApiResponse>(),
                It.IsAny<string>(),
                It.IsAny<MessageSeverity>(),
                It.IsAny<CancellationToken>()))
            .Callback<ApiResponse, string, MessageSeverity, CancellationToken>((response, code, severity, _) =>
                response.AddMessage(new ResponseMessage { Code = code, Message = code, Severity = severity }));

        translationMock
            .Setup(t => t.AddMessage(
                It.IsAny<ApiResponse>(),
                It.IsAny<string>(),
                It.IsAny<Exception>(),
                It.IsAny<CancellationToken>()))
            .Callback<ApiResponse, string, Exception, CancellationToken>((response, code, ex, _) =>
                response.AddMessage(new ResponseMessage { Code = code, Message = ex.Message, Severity = MessageSeverity.Error }));

        translationMock
            .Setup(t => t.AddMessage(
                It.IsAny<ApiResponse>(),
                It.IsAny<string>(),
                It.IsAny<Exception>(),
                It.IsAny<MessageSeverity>(),
                It.IsAny<CancellationToken>()))
            .Callback<ApiResponse, string, Exception, MessageSeverity, CancellationToken>((response, code, ex, severity, _) =>
                response.AddMessage(new ResponseMessage { Code = code, Message = ex.Message, Severity = severity }));

        translationMock
            .Setup(t => t.AddMessage(
                It.IsAny<ApiResponse>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<MessageSeverity>(),
                It.IsAny<CancellationToken>()))
            .Callback<ApiResponse, string, string, MessageSeverity, CancellationToken>((response, code, _, severity, _) =>
                response.AddMessage(new ResponseMessage { Code = code, Message = code, Severity = severity }));
    }

    public static async Task<(Guid CrudId, Guid VersionId)> SeedPublishedCrudAsync(
        IDbContext db,
        CorrelationContext correlation,
        Guid applicationId,
        Guid microserviceId,
        string code,
        string tableName = "RUNTIME_CRUD_TBL",
        Guid? crudId = null)
    {
        var resolvedCrudId = crudId ?? Guid.NewGuid();
        var fieldId = Guid.NewGuid();
        var tenantId = correlation.TenantId ?? Guid.Empty;

        var crud = new DomainCrud
        {
            Id = resolvedCrudId,
            ApplicationId = applicationId,
            TenantId = tenantId,
            MicroserviceId = microserviceId,
            Name = code,
            Code = code,
            TableName = tableName,
            Compensatible = false
        };

        db.DomainCruds.Add(crud);

        db.DomainCrudColumns.Add(new DomainCrudColumn
        {
            CrudId = resolvedCrudId,
            PropertyName = "Title",
            ColumnName = "TITLE",
            FieldId = fieldId,
            DataTypeId = Ids.DataType.String,
            SortOrder = 0
        });

        var version = new DomainCrudVersion
        {
            CorrelationId = correlation.CorrelationId,
            TenantId = tenantId,
            ApplicationId = applicationId,
            CrudId = resolvedCrudId,
            TableName = tableName,
            Version = 1,
            Active = true,
            Deleted = false,
            Created = DateOnly.FromDateTime(DateTime.UtcNow),
            CreatedTime = TimeOnly.FromDateTime(DateTime.UtcNow)
        };

        db.DomainCrudVersions.Add(version);

        db.DomainCrudVersionColumns.Add(new DomainCrudVersionColumn
        {
            CorrelationId = correlation.CorrelationId,
            CrudVersionId = version.Id,
            PropertyName = "Title",
            ColumnName = "TITLE",
            FieldId = fieldId,
            DataTypeId = Ids.DataType.String,
            SortOrder = 0
        });

        _ = await db.SaveChangesAsync(CancellationToken.None);

        return (resolvedCrudId, version.Id);
    }
}