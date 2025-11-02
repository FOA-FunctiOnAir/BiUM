using BiUM.Specialized.Mapping;
using System.Reflection;

namespace BiUM.Test.Application;

public class TestMappingProfile : MappingProfile
{
    public TestMappingProfile() : base(Assembly.GetExecutingAssembly())
    {

    }
}