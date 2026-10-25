using BiUM.Contract.Enums;
using BiUM.Contract.Models.Api;
using BiUM.Core.Common.Configs;
using BiUM.Core.Constants;
using BiUM.Core.HttpClients;
using BiUM.Specialized.Services;
using BiUM.Specialized.Services.ServiceCatalog;
using BiUM.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using System.Reflection;
using System.Text.Json;
using Xunit;

namespace BiUM.Tests.ServiceCatalog;

public sealed class ServiceCatalogEndpointsTests
{
    private sealed class SampleDetail
    {
        public Guid Id { get; set; }

        public int GradeLevel { get; set; }
    }

    private sealed class SampleController : ControllerBase
    {
        public string GetItems() => string.Empty;

        public ApiResponse<SampleDetail> GetDetail() => new();
    }

    private static readonly MethodInfo SampleAction = typeof(SampleController).GetMethod(nameof(SampleController.GetItems))!;
    private static readonly MethodInfo DetailAction = typeof(SampleController).GetMethod(nameof(SampleController.GetDetail))!;

    private static ApiDescription DetailDescription(string httpMethod, string path)
    {
        var description = new ApiDescription
        {
            HttpMethod = httpMethod,
            RelativePath = path,
            ActionDescriptor = new ControllerActionDescriptor
            {
                ControllerName = "Detail",
                ActionName = "GetDetail",
                MethodInfo = DetailAction,
                ControllerTypeInfo = typeof(SampleController).GetTypeInfo()
            }
        };

        description.ParameterDescriptions.Add(new ApiParameterDescription { Name = "countryId", Type = typeof(Guid?), Source = BindingSource.Query });

        return description;
    }

    private static ApiDescription Description(string controller, string action) => new()
    {
        HttpMethod = "GET",
        RelativePath = $"api/sample/{controller}/{action}",
        ActionDescriptor = new ControllerActionDescriptor
        {
            ControllerName = controller,
            ActionName = action,
            MethodInfo = SampleAction,
            ControllerTypeInfo = typeof(SampleController).GetTypeInfo()
        }
    };

    private static (IServiceCatalogService Service, Mock<IHttpClientsService> Http, string Domain) Create(int endpointCount, Func<List<ServiceCatalogEndpointKeyDto>, ApiResponse<List<ServiceCatalogEndpointStatusDto>>> respond, params ApiDescription[] extra)
        => Create("Test", endpointCount, respond, extra);

    private static (IServiceCatalogService Service, Mock<IHttpClientsService> Http, string Domain) Create(string environment, int endpointCount, Func<List<ServiceCatalogEndpointKeyDto>, ApiResponse<List<ServiceCatalogEndpointStatusDto>>> respond, params ApiDescription[] extra)
    {
        var descriptions = Enumerable.Range(1, endpointCount)
            .Select(index => Description($"Item{index:00}", "GetItems"))
            .Concat(extra)
            .Append(new ApiDescription
            {
                HttpMethod = "GET",
                RelativePath = "api/base/ServiceCatalog/GetEndpoints",
                ActionDescriptor = new ControllerActionDescriptor
                {
                    ControllerName = "ServiceCatalog",
                    ActionName = "GetEndpoints",
                    MethodInfo = SampleAction,
                    ControllerTypeInfo = typeof(SampleController).GetTypeInfo()
                }
            })
            .ToList();

        var provider = new Mock<IApiDescriptionGroupCollectionProvider>();

        provider.SetupGet(item => item.ApiDescriptionGroups).Returns(new ApiDescriptionGroupCollection([new ApiDescriptionGroup("sample", descriptions)], 1));

        var http = new Mock<IHttpClientsService>();

        http
            .Setup(item => item.CallService<List<ServiceCatalogEndpointStatusDto>>(
                Ids.Service.GetServiceCatalogStatuses.Id,
                It.IsAny<Dictionary<string, dynamic>?>(),
                It.IsAny<IReadOnlyList<Guid>?>(),
                It.IsAny<IReadOnlyList<Guid>?>(),
                It.IsAny<string?>(),
                It.IsAny<int?>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid _, Dictionary<string, dynamic>? parameters, IReadOnlyList<Guid>? _, IReadOnlyList<Guid>? _, string? _, int? _, int? _, CancellationToken _) =>
                respond((List<ServiceCatalogEndpointKeyDto>)parameters!["Endpoints"]));

        var serviceProvider = BiUMServiceFactory.BuildInMemory(new TestCorrelationContextProvider(), Guid.NewGuid().ToString(), services =>
        {
            services.AddSingleton(provider.Object);
            services.AddSingleton(http.Object);
            services.AddSingleton(Options.Create(new BiAppOptions { Environment = environment, Domain = "BiUM.Tests" }));
            services.AddSingleton(TranslationMock().Object);
            services.AddScoped<IServiceCatalogService, ServiceCatalogService>();
        });

        var domain = serviceProvider.GetRequiredService<IOptions<BiAppOptions>>().Value.Domain;

        return (serviceProvider.CreateScope().ServiceProvider.GetRequiredService<IServiceCatalogService>(), http, domain);
    }

    private static Mock<ITranslationService> TranslationMock()
    {
        var mock = new Mock<ITranslationService>();

        mock
            .Setup(m => m.AddMessage(It.IsAny<ApiResponse>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MessageSeverity>(), It.IsAny<CancellationToken>()))
            .Callback((ApiResponse response, string code, string exception, MessageSeverity severity, CancellationToken _) =>
                response.AddMessage(new ResponseMessage { Code = code, Message = code, Exception = exception, Severity = severity }))
            .Returns(Task.CompletedTask);

        return mock;
    }

    [Fact]
    public async Task GetManifestAsync_ShouldValidateInputAndFindEndpoint()
    {
        var (service, _, domain) = Create(2, _ => new ApiResponse<List<ServiceCatalogEndpointStatusDto>>([]), DetailDescription("GET", "api/sample/Detail/GetDetail"));

        var missing = await service.GetManifestAsync(null, "/api/sample/Detail/GetDetail", CancellationToken.None);

        missing.Messages.Should().Contain(m => m.Code == "service_catalog_endpoint_is_required");

        var notFound = await service.GetManifestAsync("GET", "/api/sample/Detail/Unknown", CancellationToken.None);

        notFound.Messages.Should().Contain(m => m.Code == "service_catalog_endpoint_not_found");

        var manifest = await service.GetManifestAsync("get", "api/sample/detail/getdetail", CancellationToken.None);

        manifest.Value!.Domain.Should().Be(domain);
        manifest.Value.Parameters.Should().ContainSingle(p => p.Name == "countryId" && p.ClrType == "Guid" && p.IsNullable);
        manifest.Value.Response.Select(p => p.Name).Should().BeEquivalentTo(["id", "gradeLevel"]);
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly Guid PreviewId = Guid.Parse("01a17063-4c44-7dae-9909-393632859b3c");

    private static void SetupPreview(Mock<IHttpClientsService> http, ApiResponse<ServiceCatalogPreviewInfoDto> response, Action<Dictionary<string, dynamic>?>? capture = null)
    {
        http
            .Setup(item => item.CallService<ServiceCatalogPreviewInfoDto>(Ids.Service.GetServicePreview.Id, It.IsAny<Dictionary<string, dynamic>?>(), It.IsAny<IReadOnlyList<Guid>?>(), It.IsAny<IReadOnlyList<Guid>?>(), It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .Callback((Guid _, Dictionary<string, dynamic>? parameters, IReadOnlyList<Guid>? _, IReadOnlyList<Guid>? _, string? _, int? _, int? _, CancellationToken _) => capture?.Invoke(parameters))
            .ReturnsAsync(response);
    }

    private static void SetupConfiguration(Mock<IHttpClientsService> http, ApiResponse<JsonElement> response, List<(Guid ServiceId, Dictionary<string, dynamic>? Parameters)> calls)
    {
        http
            .Setup(item => item.CallService<JsonElement>(It.IsAny<Guid>(), It.IsAny<Dictionary<string, dynamic>?>(), It.IsAny<IReadOnlyList<Guid>?>(), It.IsAny<IReadOnlyList<Guid>?>(), It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .Callback((Guid id, Dictionary<string, dynamic>? parameters, IReadOnlyList<Guid>? _, IReadOnlyList<Guid>? _, string? _, int? _, int? _, CancellationToken _) => calls.Add((id, parameters)))
            .ReturnsAsync(response);
    }

    private static ApiResponse<ServiceCatalogPreviewInfoDto> PreviewInfo(string url) => new(new ServiceCatalogPreviewInfoDto
    {
        PreviewId = PreviewId,
        PreviewStatus = "Completed",
        HttpMethod = "GET",
        Url = url
    });

    [Fact]
    public async Task PreviewAndApply_ShouldBeDisabledInProduction()
    {
        var (service, http, _) = Create("Production", 1, _ => new ApiResponse<List<ServiceCatalogEndpointStatusDto>>([]), DetailDescription("GET", "api/sample/Detail/GetDetail"));

        var preview = await service.PreviewAsync(new ServiceCatalogPreviewCommand { HttpMethod = "GET", Url = "/api/sample/Detail/GetDetail" }, CancellationToken.None);
        var apply = await service.ApplyAsync(new ServiceCatalogApplyCommand { PreviewId = PreviewId }, CancellationToken.None);

        preview.Messages.Should().Contain(m => m.Code == "service_catalog_disabled_in_production");
        apply.Messages.Should().Contain(m => m.Code == "service_catalog_disabled_in_production");
        JsonSerializer.Serialize(preview, JsonOptions).Should().Contain("service_catalog_disabled_in_production").And.Contain("\"value\":null");
        JsonSerializer.Serialize(apply, JsonOptions).Should().Contain("service_catalog_disabled_in_production");
        http.Verify(item => item.CallService<JsonElement>(It.IsAny<Guid>(), It.IsAny<Dictionary<string, dynamic>?>(), It.IsAny<IReadOnlyList<Guid>?>(), It.IsAny<IReadOnlyList<Guid>?>(), It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Never);
        http.Verify(item => item.CallService<ServiceCatalogPreviewInfoDto>(It.IsAny<Guid>(), It.IsAny<Dictionary<string, dynamic>?>(), It.IsAny<IReadOnlyList<Guid>?>(), It.IsAny<IReadOnlyList<Guid>?>(), It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Apply_ShouldReadPreviewAndSendRegeneratedManifestWithPreviewIdOnly()
    {
        Dictionary<string, dynamic>? previewQuery = null;
        var calls = new List<(Guid ServiceId, Dictionary<string, dynamic>? Parameters)>();
        var (service, http, _) = Create(1, _ => new ApiResponse<List<ServiceCatalogEndpointStatusDto>>([]), DetailDescription("GET", "api/sample/Detail/GetDetail"));

        SetupPreview(http, PreviewInfo("/api/sample/Detail/GetDetail"), parameters => previewQuery = parameters);
        SetupConfiguration(http, new ApiResponse<JsonElement>(JsonDocument.Parse("{}").RootElement), calls);

        var response = await service.ApplyAsync(new ServiceCatalogApplyCommand { PreviewId = PreviewId }, CancellationToken.None);

        response.Success.Should().BeTrue();
        ((Guid)previewQuery!["Id"]).Should().Be(PreviewId);

        var (serviceId, parameters) = calls.Single();

        serviceId.Should().Be(Ids.Service.ApplyInternalService.Id);
        ((ServiceCatalogManifestDto)parameters!["Manifest"]).Url.Should().Be("/api/sample/Detail/GetDetail");

        var decisions = (Dictionary<string, dynamic>)parameters["Decisions"];

        decisions.Keys.Should().Equal("PreviewId");
        ((Guid)decisions["PreviewId"]).Should().Be(PreviewId);
    }

    [Fact]
    public async Task Apply_ShouldForwardPreviewErrors_WithoutCallingApply()
    {
        var calls = new List<(Guid ServiceId, Dictionary<string, dynamic>? Parameters)>();
        var (service, http, _) = Create(1, _ => new ApiResponse<List<ServiceCatalogEndpointStatusDto>>([]), DetailDescription("GET", "api/sample/Detail/GetDetail"));
        var notFound = new ApiResponse<ServiceCatalogPreviewInfoDto>();

        notFound.AddMessage(new ResponseMessage { Code = "Configuration.service_catalog_preview_not_found", Message = "not found", Severity = MessageSeverity.Error });
        SetupPreview(http, notFound);
        SetupConfiguration(http, new ApiResponse<JsonElement>(JsonDocument.Parse("{}").RootElement), calls);

        var response = await service.ApplyAsync(new ServiceCatalogApplyCommand(), CancellationToken.None);

        response.Success.Should().BeFalse();
        JsonSerializer.Serialize(response, JsonOptions).Should().Contain("service_catalog_preview_not_found");
        calls.Should().BeEmpty();
    }

    [Fact]
    public async Task Apply_ShouldRejectPreviewOfAnotherMicroservice()
    {
        var calls = new List<(Guid ServiceId, Dictionary<string, dynamic>? Parameters)>();
        var (service, http, _) = Create(1, _ => new ApiResponse<List<ServiceCatalogEndpointStatusDto>>([]), DetailDescription("GET", "api/sample/Detail/GetDetail"));

        SetupPreview(http, PreviewInfo("/api/other/Item/GetItems"));
        SetupConfiguration(http, new ApiResponse<JsonElement>(JsonDocument.Parse("{}").RootElement), calls);

        var response = await service.ApplyAsync(new ServiceCatalogApplyCommand { PreviewId = PreviewId }, CancellationToken.None);

        response.Messages.Should().Contain(m => m.Code == "service_catalog_endpoint_not_found");
        calls.Should().BeEmpty();
    }

    [Fact]
    public async Task Apply_ShouldReturnConfigurationErrorsAsSerializableResponse()
    {
        var calls = new List<(Guid ServiceId, Dictionary<string, dynamic>? Parameters)>();
        var (service, http, _) = Create(1, _ => new ApiResponse<List<ServiceCatalogEndpointStatusDto>>([]), DetailDescription("GET", "api/sample/Detail/GetDetail"));
        var failed = new ApiResponse<JsonElement>();

        failed.AddMessage(new ResponseMessage { Code = "service_catalog_preview_outdated", Message = "outdated", Severity = MessageSeverity.Error });
        SetupPreview(http, PreviewInfo("/api/sample/Detail/GetDetail"));
        SetupConfiguration(http, failed, calls);

        var response = await service.ApplyAsync(new ServiceCatalogApplyCommand { PreviewId = PreviewId }, CancellationToken.None);

        response.Success.Should().BeFalse();

        var json = JsonSerializer.Serialize(response, JsonOptions);

        json.Should().Contain("service_catalog_preview_outdated");
        json.Should().Contain("\"value\":null");
    }

    [Fact]
    public async Task Preview_ShouldSendManifestWithoutDecisions_AndReturnOnlyIdAndStatus()
    {
        var calls = new List<(Guid ServiceId, Dictionary<string, dynamic>? Parameters)>();
        var previewId = Guid.NewGuid();
        var (service, http, _) = Create(1, _ => new ApiResponse<List<ServiceCatalogEndpointStatusDto>>([]), DetailDescription("GET", "api/sample/Detail/GetDetail"));

        http
            .Setup(item => item.CallService<ServiceCatalogPreviewStartedDto>(It.IsAny<Guid>(), It.IsAny<Dictionary<string, dynamic>?>(), It.IsAny<IReadOnlyList<Guid>?>(), It.IsAny<IReadOnlyList<Guid>?>(), It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .Callback((Guid id, Dictionary<string, dynamic>? parameters, IReadOnlyList<Guid>? _, IReadOnlyList<Guid>? _, string? _, int? _, int? _, CancellationToken _) => calls.Add((id, parameters)))
            .ReturnsAsync(new ApiResponse<ServiceCatalogPreviewStartedDto>(new ServiceCatalogPreviewStartedDto { PreviewId = previewId, PreviewStatus = "Running" }));

        var response = await service.PreviewAsync(new ServiceCatalogPreviewCommand { HttpMethod = "GET", Url = "/api/sample/Detail/GetDetail" }, CancellationToken.None);

        response.Value!.PreviewId.Should().Be(previewId);
        response.Value.PreviewStatus.Should().Be("Running");
        JsonSerializer.Serialize(response.Value, JsonOptions).Should().Be($"{{\"previewId\":\"{previewId}\",\"previewStatus\":\"Running\"}}");

        var (serviceId, parameters) = calls.Single();

        serviceId.Should().Be(Ids.Service.PreviewInternalService.Id);
        ((ServiceCatalogManifestDto)parameters!["Manifest"]).Url.Should().Be("/api/sample/Detail/GetDetail");
        parameters.ContainsKey("Decisions").Should().BeFalse();
    }

    [Fact]
    public async Task GetEndpointsAsync_ShouldPageAndAskStatusesOnlyForReturnedPage()
    {
        List<ServiceCatalogEndpointKeyDto>? asked = null;
        var serviceId = Guid.NewGuid();
        var previewId = Guid.NewGuid();

        var (service, http, _) = Create(25, endpoints =>
        {
            asked = endpoints;

            return new ApiResponse<List<ServiceCatalogEndpointStatusDto>>(
            [
                new() { HttpMethod = "GET", Url = "/api/sample/Item21/GetItems", Registered = true, ServiceId = serviceId, ServiceHttpMethod = "POST" },
                new() { HttpMethod = "GET", Url = "/API/SAMPLE/ITEM22/GETITEMS", Registered = false, PreviewStatus = "Running", PreviewId = previewId }
            ]);
        });

        var response = await service.GetEndpointsAsync(new ServiceCatalogEndpointsQuery { PageStart = 20, PageSize = 10 }, CancellationToken.None);

        response.TotalCount.Should().Be(25);
        response.PageNumber.Should().Be(3);
        response.Value!.Select(e => e.ControllerName).Should().Equal("Item21", "Item22", "Item23", "Item24", "Item25");
        asked!.Select(e => e.Url).Should().Equal(response.Value!.Select(e => e.Url));
        http.Verify(item => item.CallService<List<ServiceCatalogEndpointStatusDto>>(It.IsAny<Guid>(), It.IsAny<Dictionary<string, dynamic>?>(), It.IsAny<IReadOnlyList<Guid>?>(), It.IsAny<IReadOnlyList<Guid>?>(), It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Once);

        response.Value![0].Registered.Should().BeTrue();
        response.Value[0].ServiceId.Should().Be(serviceId);
        response.Value[0].ServiceHttpMethod.Should().Be("POST");
        response.Value[1].Registered.Should().BeFalse();
        response.Value[1].PreviewStatus.Should().Be("Running");
        response.Value[1].PreviewId.Should().Be(previewId);
        response.Value[2].Registered.Should().BeNull();
    }

    [Fact]
    public async Task GetEndpointsAsync_ShouldFilterByQAndExcludeBaseRoutes()
    {
        var (service, _, _) = Create(12, endpoints => new ApiResponse<List<ServiceCatalogEndpointStatusDto>>([]));

        var response = await service.GetEndpointsAsync(new ServiceCatalogEndpointsQuery { Q = "item1" }, CancellationToken.None);

        response.TotalCount.Should().Be(3);
        response.Value!.Select(e => e.ControllerName).Should().Equal("Item10", "Item11", "Item12");

        var all = await service.GetEndpointsAsync(new ServiceCatalogEndpointsQuery { PageSize = 100 }, CancellationToken.None);

        all.TotalCount.Should().Be(12);
        all.Value.Should().NotContain(e => e.Url.StartsWith("/api/base/"));
    }

    [Fact]
    public async Task GetEndpointsAsync_ShouldFilterByHttpMethodAndUrl()
    {
        var (service, _, _) = Create(3, _ => new ApiResponse<List<ServiceCatalogEndpointStatusDto>>([]),
            DetailDescription("POST", "api/sample/Detail/SaveDetail"),
            DetailDescription("GET", "api/sample/Detail/GetDetail"));

        var posts = await service.GetEndpointsAsync(new ServiceCatalogEndpointsQuery { HttpMethod = "post" }, CancellationToken.None);

        posts.Value!.Select(e => e.Url).Should().Equal("/api/sample/Detail/SaveDetail");

        var details = await service.GetEndpointsAsync(new ServiceCatalogEndpointsQuery { Url = "/detail/" }, CancellationToken.None);

        details.Value!.Select(e => e.HttpMethod).Should().BeEquivalentTo(["GET", "POST"]);

        var getDetail = await service.GetEndpointsAsync(new ServiceCatalogEndpointsQuery { Url = "detail", HttpMethod = "GET" }, CancellationToken.None);

        getDetail.Value!.Should().ContainSingle().Which.Url.Should().Be("/api/sample/Detail/GetDetail");
    }

    [Theory]
    [InlineData("countryid")]
    [InlineData("GRADELEVEL")]
    [InlineData("detail/get")]
    public async Task GetEndpointsAsync_ShouldSearchQInUrlAndProperties(string q)
    {
        var (service, _, _) = Create(3, _ => new ApiResponse<List<ServiceCatalogEndpointStatusDto>>([]), DetailDescription("GET", "api/sample/Detail/GetDetail"));

        var response = await service.GetEndpointsAsync(new ServiceCatalogEndpointsQuery { Q = q }, CancellationToken.None);

        response.Value!.Should().ContainSingle().Which.Url.Should().Be("/api/sample/Detail/GetDetail");
    }

    [Fact]
    public async Task GetEndpointsAsync_ShouldSendDomainAndMicroserviceId()
    {
        Dictionary<string, dynamic>? sent = null;
        var microserviceId = Guid.NewGuid();
        var (service, http, domain) = Create(2, endpoints => new ApiResponse<List<ServiceCatalogEndpointStatusDto>>([]));

        http
            .Setup(item => item.CallService<List<ServiceCatalogEndpointStatusDto>>(It.IsAny<Guid>(), It.IsAny<Dictionary<string, dynamic>?>(), It.IsAny<IReadOnlyList<Guid>?>(), It.IsAny<IReadOnlyList<Guid>?>(), It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .Callback((Guid _, Dictionary<string, dynamic>? parameters, IReadOnlyList<Guid>? _, IReadOnlyList<Guid>? _, string? _, int? _, int? _, CancellationToken _) => sent = parameters)
            .ReturnsAsync(new ApiResponse<List<ServiceCatalogEndpointStatusDto>>([]));

        await service.GetEndpointsAsync(new ServiceCatalogEndpointsQuery { MicroserviceId = microserviceId }, CancellationToken.None);

        ((Guid)sent!["MicroserviceId"]).Should().Be(microserviceId);
        ((string)sent["Domain"]).Should().Be(domain);
    }

    [Fact]
    public async Task GetEndpointsAsync_ShouldReturnPage_WhenStatusCallFails()
    {
        var (service, _, _) = Create(3, _ => throw new HttpRequestException("configuration down"));

        var response = await service.GetEndpointsAsync(new ServiceCatalogEndpointsQuery(), CancellationToken.None);

        response.Success.Should().BeTrue();
        response.Value.Should().HaveCount(3);
        response.Value.Should().OnlyContain(e => e.Registered == null);
    }
}