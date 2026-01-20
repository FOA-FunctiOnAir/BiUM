using BiUM.Infrastructure.Common.Models;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace BiApp.Test.Domain.Entities;

[Table("CURRENCY_TRANSLATION", Schema = "dbo")]
public class CurrencyTranslation : TranslationBaseEntity
{
    [JsonIgnore]
    [ForeignKey("RecordId")]
    public virtual Currency? Currency { get; set; }
}
