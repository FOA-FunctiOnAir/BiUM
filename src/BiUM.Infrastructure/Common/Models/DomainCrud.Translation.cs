using BiUM.Core.Audit;
using System.ComponentModel.DataAnnotations.Schema;

namespace BiUM.Infrastructure.Common.Models;

[Auditable(false)]
[Table("__CRUD_TRANSLATION", Schema = "dbo")]
public class DomainCrudTranslation : TranslationBaseEntity
{
    [ForeignKey("RecordId")]
    public virtual DomainCrud? DomainCrud { get; set; }
}
