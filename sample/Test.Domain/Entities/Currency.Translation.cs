using BiUM.Infrastructure.Common.Models;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace BiApp.Test.Domain.Entities;

[Table("CURRENCY_TRANSLATION", Schema = "dbo")]
public class CurrencyTranslation : TranslationBaseEntity
{
    [ForeignKey(nameof(RecordId))]
    [JsonIgnore]
    public Currency Currency { get; private set; } = null!;
}
