using AutoMapper;

namespace BiUM.Specialized.Database;

public interface IBaseRepository
{
}

public class BaseRepository : IBaseRepository
{
    public readonly IDbContext _baseContext;
    public readonly IMapper _mapper;

    public BaseRepository(IDbContext baseContext, IMapper mapper)
    {
        _baseContext = baseContext;
        _mapper = mapper;
    }

    public virtual Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _baseContext.SaveChangesAsync(cancellationToken);
    }
}