using System.ComponentModel.DataAnnotations.Schema;

namespace BiUM.Infrastructure.Common.Models;

[Table("__TRANSLATION", Schema = "dbo")]
public class DomainTranslation : BaseEntity
{
    [Column("APPLICATION_ID")]
    public Guid ApplicationId { get; set; }

    [Column("CODE")]
    public required string Code { get; set; }

    public virtual ICollection<DomainTranslationDetail>? DomainTranslationDetails { get; set; }
}