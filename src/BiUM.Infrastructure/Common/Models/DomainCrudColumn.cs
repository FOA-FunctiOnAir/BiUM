using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace BiUM.Infrastructure.Common.Models;

[Table("__CRUD_COLUMN", Schema = "dbo")]
public class DomainCrudColumn : BaseEntity
{
    [Column("CRUD_ID")]
    public Guid CrudId { get; set; }

    [Column("PROPERTY_NAME")]
    public required string PropertyName { get; set; }

    [Column("COLUMN_NAME")]
    public required string ColumnName { get; set; }

    [Column("FIELD_ID")]
    public Guid FieldId { get; set; }

    [Column("DATA_TYPE_ID")]
    public Guid DataTypeId { get; set; }

    [Column("MAX_LENGTH")]
    public int? MaxLength { get; set; }

    [Column("SORT_ORDER")]
    public int SortOrder { get; set; }


    [ForeignKey(nameof(CrudId))]
    [JsonIgnore]
    public DomainCrud DomainCrud { get; private set; } = null!;
}
