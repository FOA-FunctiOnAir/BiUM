using BiUM.Specialized.Services.DynamicExporter;
using System.Text.Json;
using Xunit;

namespace BiUM.Tests.DynamicExporter;

public class DynamicExportSourceParametersTests
{
    [Fact]
    public void SanitizeSourceParametersForStorage_StripsRoutingAndPaginationKeys()
    {
        var input = new Dictionary<string, object?>
        {
            ["componentType"] = "2fe129b1-35ee-5676-99fb-3d485c110041",
            ["microserviceId"] = "677f723e-9c8c-5ce7-b644-19d76c46df9f",
            ["pageStart"] = 0,
            ["pageSize"] = 10,
            ["dataTableInstance"] = "table-1"
        };

        var sanitized = DynamicExporterService.SanitizeSourceParametersForStorage(input);

        Assert.Single(sanitized);
        Assert.Equal("2fe129b1-35ee-5676-99fb-3d485c110041", sanitized["componentType"]);
    }

    [Fact]
    public void SanitizeSourceParametersForStorage_ExtractsScalarFromSelectObject()
    {
        var input = new Dictionary<string, object?>
        {
            ["componentType"] = JsonSerializer.Deserialize<JsonElement>("""{"id":"2fe129b1-35ee-5676-99fb-3d485c110041","name":"Business"}""")
        };

        var sanitized = DynamicExporterService.SanitizeSourceParametersForStorage(input);

        Assert.Equal("2fe129b1-35ee-5676-99fb-3d485c110041", sanitized["componentType"]);
    }
}