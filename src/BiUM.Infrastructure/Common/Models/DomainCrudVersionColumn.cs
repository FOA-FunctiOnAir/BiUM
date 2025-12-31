using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace BiUM.Infrastructure.Common.Models;

[Table("__CRUD_VERSION_COLUMN", Schema = "dbo")]
public class DomainCrudVersionColumn : BaseEntity
{
    [Column("CRUD_ID")]
    public required Guid CrudVersionId { get; set; }

    [Column("PROPERTY_NAME")]
    public required string PropertyName { get; set; }

    [Column("COLUMN_NAME")]
    public required string ColumnName { get; set; }

    [Column("FIELD_ID")]
    public required Guid FieldId { get; set; }

    [Column("DATA_TYPE_ID")]
    public required Guid DataTypeId { get; set; }

    [Column("MAX_LENGTH")]
    public int? MaxLength { get; set; }

    [Column("SORT_ORDER")]
    public int SortOrder { get; set; }


    [ForeignKey("CrudVersionId")]
    public virtual DomainCrudVersion? DomainCrudVersion { get; set; }
}
