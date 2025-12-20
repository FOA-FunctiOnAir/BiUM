using BiUM.Infrastructure.Common.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace BiUM.Test.Domain.Entities;

[Table("CURRENCY", Schema = "dbo")]
public class Currency : BaseEntity
{
    [Column("NAME")]
    public required string Name { get; set; }

    [Column("CODE")]
    public required string Code { get; set; }

    public virtual ICollection<CurrencyTranslation>? CurrencyTranslations { get; set; }
}