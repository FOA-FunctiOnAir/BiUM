using BiUM.Contract.Models.Api;
using BiUM.Core.Constants;
using BiUM.Core.HttpClients;
using BiUM.Core.MessageBroker.Events;
using BiUM.Core.MessageBroker.RabbitMQ;
using BiUM.Core.PlatformIntegration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Services.PlatformIntegration;

public sealed class PlatformActionExecutor : IPlatformActionExecutor
{
    private readonly IHttpClientsService _httpClientsService;
    private readonly IEventDefinitionProvider _eventDefinitionProvider;
    private readonly IEventIntegrationMetricRecorder _metricRecorder;
    private readonly IRabbitMQClient? _rabbitMqClient;
    private readonly IPlatformActionExecutor _self;

    public PlatformActionExecutor(
        IHttpClientsService httpClientsService,
        IEventDefinitionProvider eventDefinitionProvider,
        IEventIntegrationMetricRecorder metricRecorder,
        IEnumerable<IRabbitMQClient> rabbitMqClients)
    {
        _httpClientsService = httpClientsService;
        _eventDefinitionProvider = eventDefinitionProvider;
        _metricRecorder = metricRecorder;
        _rabbitMqClient = rabbitMqClients.FirstOrDefault();
        _self = this;
    }

    public async Task<ApiResponse> ExecuteAsync(PlatformActionRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            if (request.ActionType == Ids.Parameter.EventActionType.Values.Service ||
                request.ActionType == Ids.Parameter.SchedulerTriggerType.Values.Service)
            {
                return await ExecuteServiceAsync(request, cancellationToken);
            }

            if (request.ActionType == Ids.Parameter.EventActionType.Values.PublishEvent ||
                request.ActionType == Ids.Parameter.SchedulerTriggerType.Values.PublishEvent)
            {
                return await PublishEventAsync(request, cancellationToken);
            }

            if (request.ActionType == Ids.Parameter.EventActionType.Values.InvokeEvent ||
                request.ActionType == Ids.Parameter.SchedulerTriggerType.Values.InvokeEvent)
            {
                return await InvokeEventAsync(request, cancellationToken);
            }

            throw new InvalidOperationException("platform_action_type_unsupported");
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    private async Task<ApiResponse> ExecuteServiceAsync(PlatformActionRequest request, CancellationToken cancellationToken)
    {
        if (request.ServiceId is null || request.ServiceId == Guid.Empty)
        {
            throw new InvalidOperationException("platform_action_service_id_required");
        }

        var parameters = ToDynamicDictionary(request.Parameters);
        var response = await _httpClientsService.CallService(
            request.ServiceId.Value,
            parameters,
            cancellationToken: cancellationToken);

        await RecordMetricAsync(request.EventId, request.EventCode, response.Success, 0, null, cancellationToken);

        return response;
    }

    private async Task<ApiResponse> PublishEventAsync(PlatformActionRequest request, CancellationToken cancellationToken)
    {
        var definition = await ResolveEventDefinitionAsync(request, cancellationToken);

        if (definition is null)
        {
            throw new InvalidOperationException("platform_event_not_found");
        }

        var payload = BuildPlatformEvent(definition, request.Parameters);

        if (definition.ChannelType == Ids.Parameter.EventChannelType.Values.InternalRabbitMq ||
            definition.ChannelType == Ids.Parameter.EventChannelType.Values.ExternalRabbitMq)
        {
            await PublishRabbitMqAsync(definition, payload, cancellationToken);
        }
        else if (definition.ChannelType == Ids.Parameter.EventChannelType.Values.ExternalHttpWebhook)
        {
            await PublishWebhookAsync(definition, request.Parameters, cancellationToken);
        }
        else if (definition.ChannelType == Ids.Parameter.EventChannelType.Values.Mqtt)
        {
            throw new InvalidOperationException("platform_event_mqtt_not_implemented");
        }
        else
        {
            throw new InvalidOperationException("platform_event_channel_unsupported");
        }

        await RecordMetricAsync(definition.Id, definition.Code, true, 0, null, cancellationToken);

        return new ApiResponse();
    }

    private async Task<ApiResponse> InvokeEventAsync(PlatformActionRequest request, CancellationToken cancellationToken)
    {
        var definition = await ResolveEventDefinitionAsync(request, cancellationToken);

        if (definition is null)
        {
            throw new InvalidOperationException("platform_event_not_found");
        }

        var response = new ApiResponse();

        foreach (var action in definition.Actions.OrderBy(a => a.SortOrder))
        {
            var nested = new PlatformActionRequest
            {
                ActionType = action.ActionType,
                ServiceId = action.ServiceId,
                EventId = action.TargetEventId,
                EventCode = action.TargetEventCode,
                Parameters = new Dictionary<string, object?>(request.Parameters, StringComparer.OrdinalIgnoreCase)
            };

            var step = await _self.ExecuteAsync(nested, cancellationToken);

            if (!step.Success)
            {
                response.AddMessage(step);
                return response;
            }
        }

        await RecordMetricAsync(definition.Id, definition.Code, response.Success, 0, null, cancellationToken);

        return response;
    }

    private async Task PublishRabbitMqAsync(
        EventDefinitionDto definition,
        PlatformIntegrationEvent payload,
        CancellationToken cancellationToken)
    {
        if (_rabbitMqClient is null)
        {
            throw new InvalidOperationException("platform_action_rabbitmq_unavailable");
        }

        if (definition.ChannelType == Ids.Parameter.EventChannelType.Values.InternalRabbitMq)
        {
            await _rabbitMqClient.PublishAsync(payload, cancellationToken);
            return;
        }

        var domain = definition.RoutingKey ?? definition.Queue ?? definition.Exchange;

        if (string.IsNullOrWhiteSpace(domain))
        {
            throw new InvalidOperationException("platform_event_rabbitmq_target_missing");
        }

        await _rabbitMqClient.PublishToDomainAsync(domain.Trim(), payload, cancellationToken);
    }

    private async Task PublishWebhookAsync(
        EventDefinitionDto definition,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(definition.WebhookUrl))
        {
            throw new InvalidOperationException("platform_event_webhook_url_missing");
        }

        var response = await _httpClientsService.Post(
            definition.WebhookUrl,
            ToDynamicDictionary(parameters),
            external: true,
            cancellationToken: cancellationToken);

        if (!response.Success)
        {
            throw new InvalidOperationException("platform_event_webhook_failed");
        }
    }

    private static PlatformIntegrationEvent BuildPlatformEvent(
        EventDefinitionDto definition,
        IReadOnlyDictionary<string, object?> parameters)
    {
        var payload = new PlatformIntegrationEvent
        {
            EventId = definition.Id,
            EventCode = definition.Code
        };

        foreach (var pair in parameters)
        {
            payload.Parameters[pair.Key] = pair.Value?.ToString();
        }

        return payload;
    }

    private async Task<EventDefinitionDto?> ResolveEventDefinitionAsync(
        PlatformActionRequest request,
        CancellationToken cancellationToken)
    {
        if (request.EventId is not null && request.EventId != Guid.Empty)
        {
            return await _eventDefinitionProvider.GetByIdAsync(request.EventId.Value, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(request.EventCode))
        {
            return await _eventDefinitionProvider.GetByCodeAsync(request.EventCode, cancellationToken);
        }

        return null;
    }

    private Task RecordMetricAsync(
        Guid? eventId,
        string? eventCode,
        bool success,
        int durationMs,
        string? detail,
        CancellationToken cancellationToken) =>
        _metricRecorder.RecordAsync(
            eventId,
            eventCode,
            success
                ? Ids.Parameter.EventIntegrationStatusType.Values.Success
                : Ids.Parameter.EventIntegrationStatusType.Values.Failed,
            durationMs,
            success,
            detail,
            cancellationToken);

    private static Dictionary<string, dynamic> ToDynamicDictionary(IReadOnlyDictionary<string, object?> parameters)
    {
        var result = new Dictionary<string, dynamic>(StringComparer.OrdinalIgnoreCase);

        foreach (var pair in parameters)
        {
            result[pair.Key] = pair.Value!;
        }

        return result;
    }
}