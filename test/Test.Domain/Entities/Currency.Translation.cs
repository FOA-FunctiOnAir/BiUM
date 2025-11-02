using BiUM.Infrastructure.Common.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace BiUM.Test.Domain.Entities;

[Table("CURRENCY_TRANSLATION", Schema = "dbo")]
public class CurrencyTranslation : TranslationBaseEntity
{
    [ForeignKey("RecordId")]
    public virtual Currency? Currency { get; set; }
}