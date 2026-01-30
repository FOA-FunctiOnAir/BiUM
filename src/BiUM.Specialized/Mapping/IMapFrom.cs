namespace BiUM.Specialized.Mapping;

public interface IMapFrom<T>
{
    void Mapping(AutoMapper.Profile profile) => profile.CreateMap(typeof(T), GetType());
}