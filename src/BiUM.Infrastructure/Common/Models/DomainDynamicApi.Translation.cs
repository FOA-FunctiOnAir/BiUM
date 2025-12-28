using System.ComponentModel.DataAnnotations.Schema;

namespace BiUM.Infrastructure.Common.Models;

[Table("__DYNAMIC_API_TRANSLATION", Schema = "dbo")]
public class DomainDynamicApiTranslation : TranslationBaseEntity
{
    [ForeignKey("RecordId")]
    public virtual DomainDynamicApi? DynamicApi { get; set; }
}
