using BiUM.Contract.Models.Api;
using BiUM.Core.HttpClients;
using BiUM.Core.PlatformIntegration;
using BiUM.Infrastructure.Services.PlatformIntegration;
using FluentAssertions;
using Moq;
using Xunit;

namespace BiUM.Tests.PlatformIntegration;

public sealed class HttpEventDefinitionProviderTests
{
    [Fact]
    public async Task GetByCodeAsync_calls_configuration_fw_endpoint()
    {
        var expected = new EventDefinitionDto
        {
            Id = Guid.NewGuid(),
            ApplicationId = Guid.NewGuid(),
            Code = "order-created",
            ChannelType = Guid.NewGuid(),
            DirectionType = Guid.NewGuid()
        };

        Dictionary<string, dynamic>? captured = null;
        var http = new Mock<IHttpClientsService>();
        http.Setup(h => h.Get<EventDefinitionDto>(
                "/api/configuration/Event/GetFwEventDefinition",
                It.IsAny<Dictionary<string, dynamic>>(),
                It.IsAny<bool>(),
                It.IsAny<string?>(),
                It.IsAny<int?>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, Dictionary<string, dynamic>?, bool, string?, int?, int?, CancellationToken>((_, parameters, _, _, _, _, _) =>
                captured = parameters)
            .ReturnsAsync(new ApiResponse<EventDefinitionDto> { Value = expected });

        var provider = new HttpEventDefinitionProvider(http.Object);

        var result = await provider.GetByCodeAsync("order-created");

        result.Should().BeSameAs(expected);
        captured.Should().NotBeNull();
        ((string)captured!["code"]).Should().Be("order-created");
    }
}