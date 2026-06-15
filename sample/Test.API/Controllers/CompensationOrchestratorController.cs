using BiUM.Contract.Models.Api;
using BiUM.Core.HttpClients;
using BiUM.Specialized.Common.API;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BiApp.Test.API.Controllers;

/// <summary>
/// Compensation mekanizmasını test etmek için orchestrator endpoint.
///
/// [CompensatableApi] + dışarıdan session gelmemesi → CompensatableApiActionFilter
/// otomatik session açar ve API bitiminde commit/rollback yapar.
///
/// Akış:
///   1. Filter: yeni CompensationSessionId oluşturur, CorrelationContext'e set eder.
///   2. SaveCompensationItem çağrılır → IHttpClientsService x-correlation-context header'ını taşır.
///      → Sample2 DB: COMPENSATION_ITEM.C_STATUS = "I", COMPENSATION_SESSION_ID = sessionId
///         __COMPENSATION_SNAPSHOT: State = Pending
///   3. shouldFail = false → API başarılı döner → filter CommitSessionAsync + PublishAsync(success=true)
///      → RabbitMQ → Sample2 CompensationSessionFinalizedHandler → CommitSessionAsync
///      → C_STATUS = "C", COMPENSATION_SESSION_ID = null
///   4. shouldFail = true → API failure döner → filter RollbackSessionAsync + PublishAsync(success=false)
///      → RabbitMQ → Sample2 handler → RollbackSessionAsync → kayıt silinir
///
/// Debug noktaları:
///   A) innerResult döndükten hemen sonra: Sample2 DB → C_STATUS = "I"
///   B) API cevap verdikten sonra (RabbitMQ event işlendikten sonra): C_STATUS = "C" veya kayıt yok
/// </summary>
[BiUMRoute("test")]
public class CompensationOrchestratorController : ApiControllerBase
{
    // appsettings.Local.json → HttpClientsOptions.Domains["test2"] = "http://localhost:5001"
    private const string SaveCompensationItemPath = "/api/test2/compensationtest/savecompensationitem";

    private readonly IHttpClientsService _httpClientsService;

    public CompensationOrchestratorController(IHttpClientsService httpClientsService)
    {
        _httpClientsService = httpClientsService;
    }

    /// <param name="itemName">Sample2'de oluşturulacak CompensationItem.Name</param>
    /// <param name="shouldFail">true → rollback, false → commit</param>
    [HttpPost]
    [CompensatableApi]
    public async Task<ApiResponse> TestCompensation(
        [FromQuery] string itemName = "Test Item",
        [FromQuery] bool shouldFail = false,
        CancellationToken cancellationToken = default)
    {
        // ── Debug breakpoint A: bu satırdan önce DB'ye bak, henüz hiçbir şey yazılmadı ──

        var innerResult = await _httpClientsService.Post(
            SaveCompensationItemPath,
            new Dictionary<string, dynamic>
            {
                ["id"] = Guid.NewGuid(),
                ["name"] = itemName,
                ["test"] = false,
            },
            cancellationToken: cancellationToken);

        // ── Debug breakpoint B: Sample2 DB'sinde C_STATUS = "I" görünmeli ────────────────

        if (!innerResult.Success)
        {
            return Fail("inner_call_failed");
        }

        if (shouldFail)
        {
            return Fail("rolled_back_intentionally");
        }

        return new ApiResponse();
        // ── API döndükten sonra [CompensatableApiActionFilter] devreye girer:
        //    success → CommitSession + Publish(true)  → Sample2 C_STATUS = "C"
        //    failure → RollbackSession + Publish(false) → Sample2 kayıt silinir
    }

    private static ApiResponse Fail(string code)
    {
        var response = new ApiResponse();
        response.AddMessage(new BiUM.Contract.Models.Api.ResponseMessage
        {
            Code = code,
            Message = code,
            Severity = BiUM.Contract.Enums.MessageSeverity.Error
        });
        return response;
    }
}