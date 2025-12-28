using BiUM.Infrastructure.Common.Models;
using BiUM.Specialized.Common.Models;
using BiUM.Specialized.Mapping;
using System;

namespace BiUM.Specialized.Common.Crud;

public class DomainCrudColumnDto : BaseDto, IMapFrom<DomainCrudColumn>
{
    public Guid CrudId { get; set; }
    public string? PropertyName { get; set; }
    public string? ColumnName { get; set; }
    public Guid FieldId { get; set; }
    public Guid DataTypeId { get; set; }
    public int? MaxLength { get; set; }
    public int SortOrder { get; set; }
}
