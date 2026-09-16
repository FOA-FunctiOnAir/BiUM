using System;
using System.Collections.Generic;

namespace BiUM.Core.PlatformIntegration;

public sealed class EventDefinitionDto
{
    public Guid Id { get; init; }

    public Guid ApplicationId { get; init; }

    public Guid? MicroserviceId { get; init; }

    public required string Code { get; init; }

    public Guid ChannelType { get; init; }

    public Guid DirectionType { get; init; }

    public Guid? EventCredentialId { get; init; }

    public string? Exchange { get; init; }

    public string? Queue { get; init; }

    public string? RoutingKey { get; init; }

    public string? WebhookUrl { get; init; }

    public string? MqttTopic { get; init; }

    public EventCredentialDefinitionDto? Credential { get; init; }

    public IReadOnlyList<EventParameterDefinitionDto> Parameters { get; init; } = [];

    public IReadOnlyList<EventActionDefinitionDto> Actions { get; init; } = [];
}

public sealed class EventParameterDefinitionDto
{
    public Guid DirectionType { get; init; }

    public required string Property { get; init; }

    public Guid FieldId { get; init; }
}

public sealed class EventActionDefinitionDto
{
    public Guid ActionType { get; init; }

    public int SortOrder { get; init; }

    public Guid? ServiceId { get; init; }

    public Guid? TargetEventId { get; init; }

    public string? TargetEventCode { get; init; }
}

public sealed class EventCredentialDefinitionDto
{
    public Guid Id { get; init; }

    public Guid CredentialType { get; init; }

    public string? Username { get; init; }

    public string? Password { get; init; }

    public string? ClientId { get; init; }

    public string? ClientSecret { get; init; }

    public string? ApiKey { get; init; }

    public string? ApiKeyHeaderName { get; init; }

    public string? WebhookSecret { get; init; }

    public string? MqttHost { get; init; }

    public int? MqttPort { get; init; }
}