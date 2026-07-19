using BiUM.Core.Common.Configs;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace BiUM.Tests.Extensions;

public class HostEnvironmentSwaggerExtensionsTests
{
    [Theory]
    [InlineData("Production", "Development", false)]
    [InlineData("Production", "Production", false)]
    [InlineData("Sandbox", "Production", false)]
    [InlineData("Staging", "Production", false)]
    [InlineData("QA", "Production", false)]
    [InlineData("Development", "Production", true)]
    [InlineData("PreDevelopment", "Production", true)]
    [InlineData("Development", "Development", true)]
    public void IsSwaggerEnabled_respects_BiAppOptions_over_host(
        string biAppEnvironment,
        string hostEnvironment,
        bool expected)
    {
        var host = new TestHostEnvironment { EnvironmentName = hostEnvironment };
        var options = new BiAppOptions { Environment = biAppEnvironment, Domain = "Test" };

        Assert.Equal(expected, host.IsSwaggerEnabled(options));
    }

    [Fact]
    public void IsSwaggerEnabled_when_BiAppOptions_missing_uses_host_development_only()
    {
        var productionHost = new TestHostEnvironment { EnvironmentName = "Production" };
        var developmentHost = new TestHostEnvironment { EnvironmentName = "Development" };

        Assert.False(productionHost.IsSwaggerEnabled(null));
        Assert.True(developmentHost.IsSwaggerEnabled(null));
    }

    [Theory]
    [InlineData("Development", "Production", true)]
    [InlineData("Development", "Development", true)]
    [InlineData("Production", "Development", false)]
    public void IsSwaggerUiEnabled_requires_swagger_and_development_like_context(
        string biAppEnvironment,
        string hostEnvironment,
        bool expected)
    {
        var host = new TestHostEnvironment { EnvironmentName = hostEnvironment };
        var options = new BiAppOptions { Environment = biAppEnvironment, Domain = "Test" };

        Assert.Equal(expected, host.IsSwaggerUiEnabled(options));
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Production";

        public string ApplicationName { get; set; } = "Test";

        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}