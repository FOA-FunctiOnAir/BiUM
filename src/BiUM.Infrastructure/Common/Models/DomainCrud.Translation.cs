using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace BiUM.Infrastructure.Common.Models;

[Table("__CRUD_TRANSLATION", Schema = "dbo")]
public class DomainCrudTranslation : TranslationBaseEntity
{
    [ForeignKey(nameof(RecordId))]
    [JsonIgnore]
    public DomainCrud DomainCrud { get; private set; } = null!;
}