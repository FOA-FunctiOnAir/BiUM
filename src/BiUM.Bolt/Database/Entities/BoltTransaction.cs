using BiUM.Core.Audit;
using BiUM.Infrastructure.Common.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace BiUM.Bolt.Database.Entities;

[Auditable(false)]
[Table("__BOLT_TRANSACTION", Schema = "dbo")]
public class BoltTransaction : BaseEntity
{
    [Column("TABLE_NAME")]
    public required string TableName { get; set; }

    [Column("IDS")]
    public string? Ids { get; set; }

    [Column("DELETE")]
    public required bool Delete { get; set; }

    [Column("SORT_ORDER")]
    public required int SortOrder { get; set; }
}
