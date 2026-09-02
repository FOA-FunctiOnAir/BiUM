using BiUM.Specialized.Common.Utils;
using FluentAssertions;
using Xunit;

namespace BiUM.Tests.DynamicApi;

public class MicroserviceCodeHelperTests
{
    [Theory]
    [InlineData("/api/education/coach", "education-coach")]
    [InlineData("api/customers", "customers")]
    [InlineData("/api/bpmn/", "bpmn")]
    [InlineData(null, "")]
    public void FromRootPath_normalizes_microservice_code(string? rootPath, string expected)
    {
        MicroserviceCodeHelper.FromRootPath(rootPath).Should().Be(expected);
    }
}