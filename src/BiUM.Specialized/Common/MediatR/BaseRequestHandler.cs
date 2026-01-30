namespace BiUM.Specialized.Common.MediatR;

public interface IBaseRequestHandler<TRequest>
{
}

public interface IBaseRequestHandler<TRequest, TType> : IBaseRequestHandler<TRequest>
{
}