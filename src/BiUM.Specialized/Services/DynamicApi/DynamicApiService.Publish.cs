using BiUM.Contract.Enums;
using BiUM.Contract.Models.Api;
using BiUM.Core.Constants;
using BiUM.Infrastructure.Common.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Services.DynamicApi;

public partial class DynamicApiService
{
    public virtual async Task<ApiResponse> PublishDomainDynamicApiAsync(Guid id, CancellationToken cancellationToken)
    {
        var response = new ApiResponse();

        var domainDynamicApi = await DbContext.DomainDynamicApis
            .Include(x => x.DynamicApiParameters)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (domainDynamicApi is null)
        {
            await AddMessage(response, "dynamic_api_definition_not_found", cancellationToken);
            return response;
        }

        if (!CanMutateDefinition(domainDynamicApi))
        {
            await AddMessage(response, "dynamic_api_definition_access_denied", cancellationToken);
            return response;
        }

        if (domainDynamicApi.ApplicationId == Guid.Empty || domainDynamicApi.MicroserviceId == Guid.Empty)
        {
            await AddMessage(response, "dynamic_api_application_or_microservice_is_required", cancellationToken);
            return response;
        }

        if (string.IsNullOrWhiteSpace(domainDynamicApi.Code) || string.IsNullOrWhiteSpace(domainDynamicApi.SourceCode))
        {
            await AddMessage(response, "dynamic_api_code_and_source_required", cancellationToken);
            return response;
        }

        if (!await ValidateAndSyncTableReferencesAsync(
                domainDynamicApi,
                domainDynamicApi.ApplicationId,
                domainDynamicApi.MicroserviceId,
                domainDynamicApi.SourceCode,
                requirePhysicalTable: true,
                response,
                cancellationToken))
        {
            domainDynamicApi.CompileStatusType = Ids.Parameter.DynamicApiCompileStatus.Values.Failed;
            domainDynamicApi.CompileError = string.Join("; ", response.Messages.Select(m => m.Message));
            _ = DbContext.DomainDynamicApis.Update(domainDynamicApi);
            _ = await DbContext.SaveChangesAsync(cancellationToken);
            await AddMessage(response, "dynamic_api_compile_failed", cancellationToken);
            return response;
        }

        var parseResult = DynamicApiEntityParser.Parse(domainDynamicApi.SourceCode);
        var additionalSources = new List<string>();
        var usesDynamicTables = parseResult.TableReferences.Count > 0;

        if (usesDynamicTables)
        {
            var created = await CreateIntrospectorAsync(cancellationToken);

            if (created is null)
            {
                await AddMessage(response, "dynamic_api_table_introspection_failed", cancellationToken);
                return response;
            }

            var entityModels = new List<(DynamicApiTableReference Reference, IReadOnlyList<DynamicApiColumnMetadata> Columns)>();

            foreach (var reference in parseResult.TableReferences)
            {
                var columns = await created.GetColumnsAsync(
                    reference.Schema,
                    reference.TableName,
                    cancellationToken);

                if (columns.Count == 0)
                {
                    await AddMessage(response, "dynamic_api_table_introspection_failed", cancellationToken);
                    return response;
                }

                entityModels.Add((reference, columns));
                additionalSources.Add(DynamicApiEntityCodegen.GenerateEntitySource(reference, columns));
            }

            additionalSources.Add(DynamicApiEntityCodegen.GenerateDbContextSource(entityModels));
            additionalSources.Add(DynamicApiEntityCodegen.GenerateDbContextFactorySource());
        }

        var domainDbContextTypeFullName = usesDynamicTables ? null : DbContext.GetType().FullName;

        var preparedSource = DynamicApiHandlerSourceNormalizer.PrepareForCompile(
            domainDynamicApi.SourceCode,
            domainDbContextTypeFullName,
            usesDynamicTables);

        var transformedSource = usesDynamicTables
            ? DynamicApiSourceTransformer.Transform(preparedSource, parseResult.TableReferences)
            : preparedSource;

        var compileResult = DynamicApiCompiler.Compile(new DynamicApiCompileRequest
        {
            HandlerSourceCode = transformedSource,
            TypeNameSeed = $"{domainDynamicApi.Code}_{domainDynamicApi.Id:N}",
            UsesDynamicTables = usesDynamicTables,
            DomainDbContextTypeFullName = domainDbContextTypeFullName,
            AdditionalSourceFiles = additionalSources
        });

        if (!compileResult.Success)
        {
            domainDynamicApi.CompileStatusType = Ids.Parameter.DynamicApiCompileStatus.Values.Failed;
            domainDynamicApi.CompileError = compileResult.Error;
            _ = DbContext.DomainDynamicApis.Update(domainDynamicApi);
            _ = await DbContext.SaveChangesAsync(cancellationToken);
            await AddMessage(response, "dynamic_api_compile_failed", cancellationToken);
            response.AddMessage(new ResponseMessage
            {
                Code = "dynamic_api_compile_error",
                Message = compileResult.Error ?? string.Empty,
                Severity = MessageSeverity.Error
            });
            return response;
        }

        var lastVersion = await DbContext.DomainDynamicApiVersions
            .Where(x => x.DynamicApiId == domainDynamicApi.Id)
            .OrderByDescending(x => x.Version)
            .FirstOrDefaultAsync(cancellationToken);

        var newVersionNumber = (lastVersion?.Version ?? 0) + 1;

        var version = new DomainDynamicApiVersion
        {
            DynamicApiId = domainDynamicApi.Id,
            Code = domainDynamicApi.Code,
            Version = newVersionNumber,
            HttpType = domainDynamicApi.HttpType,
            ExecutionType = domainDynamicApi.ExecutionType,
            RuntimePlatformType = domainDynamicApi.RuntimePlatformType,
            SourceCode = domainDynamicApi.SourceCode,
            CompiledAssembly = compileResult.AssemblyBytes,
            AssemblyHash = compileResult.AssemblyHash,
            EntryPointTypeName = compileResult.EntryPointTypeName
        };

        foreach (var param in domainDynamicApi.DynamicApiParameters)
        {
            DbContext.DomainDynamicApiVersionParameters.Add(new DomainDynamicApiVersionParameter
            {
                DynamicApiVersionId = version.Id,
                DirectionType = param.DirectionType,
                Property = param.Property,
                FieldId = param.FieldId
            });
        }

        domainDynamicApi.CompileStatusType = Ids.Parameter.DynamicApiCompileStatus.Values.Success;
        domainDynamicApi.CompileError = null;

        _ = DbContext.DomainDynamicApiVersions.Add(version);
        _ = DbContext.DomainDynamicApis.Update(domainDynamicApi);

        var saveServicesResponse = await SaveDynamicApiServicesAsync(
            domainDynamicApi.ApplicationId,
            domainDynamicApi.MicroserviceId,
            domainDynamicApi.Code,
            domainDynamicApi.HttpType,
            domainDynamicApi.DynamicApiParameters,
            cancellationToken);

        if (!saveServicesResponse.Success)
        {
            response.AddMessage(saveServicesResponse.Messages);
            return response;
        }

        _runtimeCache.Invalidate(BuildCacheKey(domainDynamicApi.Code, version.Version));
        _ = await DbContext.SaveChangesAsync(cancellationToken);

        return response;
    }

    protected virtual async Task<ApiResponse> SaveDynamicApiServicesAsync(
        Guid applicationId,
        Guid microserviceId,
        string code,
        Guid httpType,
        IEnumerable<DomainDynamicApiParameter> parameters,
        CancellationToken cancellationToken)
    {
        var response = new ApiResponse();

        var parameterPayload = parameters.Select(p => new Dictionary<string, dynamic>
        {
            { "Property", p.Property },
            { "FieldId", p.FieldId },
            { "DirectionType", p.DirectionType }
        }).ToList();

        var payload = new Dictionary<string, dynamic>
        {
            { "ApplicationId", applicationId },
            { "MicroserviceId", microserviceId },
            { "Code", code },
            { "HttpType", httpType },
            { "Parameters", parameterPayload }
        };

        var responseApi = await _httpClientsService.CallService<ApiResponse>(
            serviceId: Ids.Service.SaveDynamicApiServices.Id,
            parameters: payload,
            cancellationToken: cancellationToken);

        if (!responseApi.Success)
        {
            response.AddMessage(responseApi.Messages);
        }

        return response;
    }

    private static string BuildCacheKey(string code, int version) => $"dynamic-api:{code}:{version}";
}