using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace BiUM.Infrastructure.Common.Models;

[Table("__DYNAMIC_API_TRANSLATION", Schema = "dbo")]
public class DomainDynamicApiTranslation : TranslationBaseEntity
{
    [ForeignKey(nameof(RecordId))]
    [JsonIgnore]
    public DomainDynamicApi DomainDynamicApi { get; private set; } = null!;
}