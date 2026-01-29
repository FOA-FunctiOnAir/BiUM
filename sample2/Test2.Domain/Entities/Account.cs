using AutoMapper;
using BiApp.Test2.Domain.Events;
using BiUM.Core.MessageBroker;
using BiUM.Infrastructure.Common.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace BiApp.Test2.Domain.Entities;

[Table("ACCOUNT", Schema = "dbo")]
public class Account : BaseEntity
{
    [Column("NAME")]
    public required string Name { get; set; }

    [Column("CODE")]
    public required string Code { get; set; }

    [JsonIgnore]
    public ICollection<AccountTranslation> AccountTranslations { get; set; } = [];

    public override IBaseEvent? AddCreatedEvent(IMapper? mapper, IBaseEvent? baseEvent)
    {
        var createdEvent = mapper?.Map<AccountCreatedEvent>(this);

        return base.AddCreatedEvent(mapper, createdEvent);
    }

    public override IBaseEvent? AddUpdatedEvent(IMapper? mapper, IBaseEvent? baseEvent)
    {
        var createdEvent = mapper?.Map<AccountUpdatedEvent>(this);

        return base.AddUpdatedEvent(mapper, createdEvent);
    }

    public override IBaseEvent? AddDeletedEvent(IMapper? mapper, IBaseEvent? baseEvent)
    {
        var createdEvent = mapper?.Map<AccountDeletedEvent>(this);

        return base.AddDeletedEvent(mapper, createdEvent);
    }
}
