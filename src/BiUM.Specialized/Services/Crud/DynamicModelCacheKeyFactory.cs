//using Microsoft.EntityFrameworkCore.Infrastructure;
//using Microsoft.EntityFrameworkCore;

//namespace BiUM.Specialized.Services.Crud;

//public sealed class DynamicModelCacheKeyFactory : IModelCacheKeyFactory
//{
//    private readonly ICrudVersionAccessor _ver;
//    public DynamicModelCacheKeyFactory(ICrudVersionAccessor ver) => _ver = ver;

//    public object Create(DbContext context, bool designTime)
//        => (context.GetType(), _ver.CurrentVersion, designTime);
//}