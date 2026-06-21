using System;

namespace BiUM.Specialized.Common.Mapper;

public class BaseForValuesDto<TType>
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
}