using BiUM.Specialized.Mapping;
using System;

namespace BiUM.Specialized.Common.Mapper;

public class BaseForValuesDto<TType> : IMapFrom<TType>
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
}