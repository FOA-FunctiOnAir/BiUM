using BiUM.Infrastructure.Common.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace BiUM.Bolt.Database.Entities;

[Table("__BOLT_TRANSACTION", Schema = "dbo")]
public class BoltTransaction : BaseEntity
{
    [Column("TABLE_NAME")]
    public required string TableName { get; set; }

    [Column("IDS")]
    public string? Ids { get; set; }
}