using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace BiUM.Infrastructure.Common.Models;

[Table("__TRANSLATION", Schema = "dbo")]
public class DomainTranslation : BaseEntity
{
    [Column("APPLICATION_ID")]
    public Guid ApplicationId { get; set; }

    [Column("CODE")]
    public required string Code { get; set; }

    [ForeignKey(nameof(DomainTranslationDetail.TranslationId))]
    [JsonIgnore]
    public ICollection<DomainTranslationDetail> DomainTranslationDetails { get; } = [];
}
