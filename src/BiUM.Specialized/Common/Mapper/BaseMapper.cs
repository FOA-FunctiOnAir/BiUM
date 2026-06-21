using BiUM.Specialized.Mapping;
using System;

namespace BiUM.Specialized.Common.Mapper;

public abstract class ForValuesDtoBase
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
}

public class BaseForValuesDto<TEntity> : ForValuesDtoBase, IMapFrom<TEntity>
{
}