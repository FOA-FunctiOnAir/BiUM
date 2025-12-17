using BiUM.Specialized.Mapping;
using System;

namespace BiUM.Specialized.Common.Mapper;

public class BaseForValuesDto<TType> : IMapFrom<TType>
{
    public Guid Id { get; set; }

    public string? Name { get; set; }
}