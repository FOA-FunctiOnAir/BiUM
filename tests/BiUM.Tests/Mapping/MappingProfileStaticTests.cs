using AutoMapper;
using BiUM.Specialized.Mapping;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BiUM.Tests.Mapping;

public class MappingProfileStaticTests
{
    public class Source { public string? Value { get; set; } }

    public class StaticMappingDto : IMapFrom<Source>
    {
        public string? Value { get; set; }

        public static void Mapping(Profile profile)
        {
            profile.CreateMap<Source, StaticMappingDto>();
        }
    }

    public class InstanceMappingDto : IMapFrom<Source>
    {
        public string? Value { get; set; }

        public void Mapping(Profile profile)
        {
            profile.CreateMap<Source, InstanceMappingDto>();
        }
    }

    [Fact]
    public void Static_Mapping_method_is_discovered_and_map_is_registered()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile(new MappingProfile(typeof(StaticMappingDto).Assembly)),
            NullLoggerFactory.Instance);

        var mapper = config.CreateMapper();

        var result = mapper.Map<StaticMappingDto>(new Source { Value = "hello" });

        Assert.Equal("hello", result.Value);
    }

    [Fact]
    public void Instance_Mapping_method_still_works()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile(new MappingProfile(typeof(InstanceMappingDto).Assembly)),
            NullLoggerFactory.Instance);

        var mapper = config.CreateMapper();

        var result = mapper.Map<InstanceMappingDto>(new Source { Value = "world" });

        Assert.Equal("world", result.Value);
    }

    [Fact]
    public void Both_static_and_instance_mappings_can_coexist()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile(new MappingProfile(typeof(StaticMappingDto).Assembly)),
            NullLoggerFactory.Instance);

        var mapper = config.CreateMapper();

        var staticResult = mapper.Map<StaticMappingDto>(new Source { Value = "s" });
        var instanceResult = mapper.Map<InstanceMappingDto>(new Source { Value = "i" });

        Assert.Equal("s", staticResult.Value);
        Assert.Equal("i", instanceResult.Value);
    }
}