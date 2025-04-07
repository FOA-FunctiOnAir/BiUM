using MediatR;

namespace BiUM.Specialized.Common.MediatR;

public record BaseRequestDto<TType> : IRequest<TType>
{
}