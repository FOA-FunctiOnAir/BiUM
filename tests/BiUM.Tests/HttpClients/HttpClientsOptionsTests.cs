using BiUM.Core.Common.Configs;
using FluentAssertions;
using Xunit;

namespace BiUM.Tests.HttpClients;

public sealed class HttpClientsOptionsTests
{
    private static HttpClientsOptions CreateOptions() => new()
    {
        Environment = "Development",
        BaseUrl = "http://localhost",
        Domains = new Dictionary<string, string>
        {
            ["configuration"] = "http://configuration",
            ["education-assessment"] = "http://education-assessment",
            ["education-coach"] = "http://education-coach",
            ["humanresources-absence"] = "http://humanresources-absence",
            ["observability"] = "http://observability"
        }
    };

    [Theory]
    [InlineData("education/assessment", "education-assessment")]
    [InlineData("/api/education/coach/", "education-coach")]
    [InlineData("HumanResources.Absence", "humanresources-absence")]
    [InlineData("observability ", "observability")]
    [InlineData("configuration", "configuration")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void ToDomainKey_ShouldMatchDomainMapKeys(string? rootPath, string expected)
    {
        HttpClientsOptions.ToDomainKey(rootPath).Should().Be(expected);
    }

    [Theory]
    [InlineData("education/assessment", "http://education-assessment/api/base/Crud/GetList/visit")]
    [InlineData("education/coach", "http://education-coach/api/base/Crud/GetList/visit")]
    [InlineData("configuration", "http://configuration/api/base/Crud/GetList/visit")]
    public void GetFullUrl_ShouldResolveBaseUrlsOfMultiSegmentRootPaths(string rootPath, string expected)
    {
        CreateOptions().GetFullUrl(rootPath, "/api/base/Crud/GetList/visit").Should().Be(expected);
    }

    [Fact]
    public void GetFullUrl_ShouldStillResolveDomainUrlsWithRootPath()
    {
        CreateOptions().GetFullUrl("education/assessment", "/api/education/assessment/Question/ApproveQuestions")
            .Should().Be("http://education-assessment/api/education/assessment/Question/ApproveQuestions");
    }

    [Fact]
    public void GetFullUrl_ShouldThrowWhenBaseRootPathIsUnknown()
    {
        var act = () => CreateOptions().GetFullUrl("", "/api/base/Crud/GetList/visit");

        act.Should().Throw<InvalidOperationException>();
    }
}