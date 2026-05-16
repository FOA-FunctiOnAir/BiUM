using BiUM.Infrastructure.Common.Models;
using BiUM.Specialized.Mapping;
using System;
using System.Collections.Generic;

namespace BiUM.Specialized.Common.Crud;

public class DomainCrudPartialUpdateDto : BaseDto, IMapFrom<DomainCrudPartialUpdate>
{
    public Guid CrudId { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public ICollection<DomainCrudPartialUpdateColumnDto> Columns { get; set; } = [];
}