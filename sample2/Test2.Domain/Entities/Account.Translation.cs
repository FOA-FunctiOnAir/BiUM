using BiUM.Infrastructure.Common.Models;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace BiApp.Test2.Domain.Entities;

[Table("ACCOUNT_TRANSLATION", Schema = "dbo")]
public class AccountTranslation : TranslationBaseEntity
{
    [ForeignKey(nameof(RecordId))]
    [JsonIgnore]
    public Account Account { get; private set; } = null!;
}