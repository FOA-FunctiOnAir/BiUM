using BiUM.Core.Authorization;
using BiUM.Core.Caching.InMemory;
using BiUM.Core.Caching.Redis;
using BiUM.Core.Common.Configs;
using BiUM.Core.Constants;
using BiUM.Core.Serialization;
using BiUM.Infrastructure.Services.HttpClients;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace BiUM.Tests.HttpClients;

public sealed class TargetMicroserviceCallTests
{
    private static readonly Guid AssessmentId = Guid.NewGuid();
    private static readonly Guid SaveCrudServiceId = Guid.NewGuid();

    private sealed class StubHandler : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);

            var path = request.RequestUri!.AbsolutePath;

            var body = path switch
            {
                "/api/configuration/Microservice/GetMicroservice" =>
                    $$"""{"value":{"id":"{{AssessmentId}}","name":"Education Assessment","rootPath":"education/assessment"},"messages":[],"success":true}""",
                "/api/configuration/Service/GetService" =>
                    $$"""{"value":{"id":"{{SaveCrudServiceId}}","microserviceId":"{{Guid.NewGuid()}}","microserviceRootPath":"uiframework","type":"{{Ids.Parameter.ServiceType.Values.Internal}}","name":"Save Domain Crud","url":"/api/base/DomainCrud/SaveDomainCrud","httpType":"{{Ids.Parameter.HttpType.Values.Post}}"},"messages":[],"success":true}""",
                _ => """{"value":7,"messages":[],"success":true}"""
            };

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
        }
    }

    private sealed class StubFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, false);
    }

    private static (HttpClientService Service, StubHandler Handler) Create()
    {
        var handler = new StubHandler();
        var environment = new Mock<IHostEnvironment>();
        var jsonOptions = new JsonSerializerOptions();

        environment.SetupGet(item => item.EnvironmentName).Returns("Development");
        BiJsonOptions.Configure(jsonOptions);

        var service = new HttpClientService(
            new StubFactory(handler),
            Mock.Of<IHttpContextAccessor>(),
            environment.Object,
            Mock.Of<ICorrelationContextAccessor>(),
            Mock.Of<ICorrelationContextSerializer>(),
            jsonOptions,
            Options.Create(new BiAppOptions { Environment = "Development", Domain = "AiAssistant" }),
            Options.Create(new HttpClientsOptions
            {
                Environment = "Development",
                BaseUrl = "http://localhost",
                Domains = new Dictionary<string, string>
                {
                    ["configuration"] = "http://configuration",
                    ["education-assessment"] = "http://education-assessment",
                    ["uiframework"] = "http://uiframework"
                }
            }),
            Array.Empty<IRedisClient>(),
            Array.Empty<IInMemoryClient>(),
            new MemoryCache(new MemoryCacheOptions()),
            NullLogger<HttpClientService>.Instance);

        return (service, handler);
    }

    [Fact]
    public async Task Get_ShouldSendBaseUrlToTargetMicroservice()
    {
        var (service, handler) = Create();

        var result = await service.Get<int>("/api/base/DomainCrud/GetDomainCruds", AssessmentId);

        result.Success.Should().BeTrue();
        handler.Requests.Last().RequestUri!.ToString().Should().StartWith("http://education-assessment/api/base/DomainCrud/GetDomainCruds");
    }

    [Fact]
    public async Task Post_ShouldRejectNonBaseUrlsForTargetMicroservice()
    {
        var (service, handler) = Create();

        var result = await service.Post("/api/configuration/Field/SaveField", AssessmentId);

        result.Success.Should().BeFalse();
        handler.Requests.Should().ContainSingle(request => request.RequestUri!.AbsolutePath == "/api/configuration/Microservice/GetMicroservice");
    }

    [Fact]
    public async Task CallService_ShouldUseCatalogUrlWithTargetMicroserviceRootPath()
    {
        var (service, handler) = Create();

        var result = await service.CallService(SaveCrudServiceId, AssessmentId, new Dictionary<string, dynamic> { ["code"] = "visit" });

        result.Success.Should().BeTrue();

        var call = handler.Requests.Last();

        call.Method.Should().Be(HttpMethod.Post);
        call.RequestUri!.ToString().Should().Be("http://education-assessment/api/base/DomainCrud/SaveDomainCrud");
    }

    [Fact]
    public async Task Get_WithoutTarget_ShouldKeepDomainResolution()
    {
        var (service, handler) = Create();

        await service.Get<int>("/api/configuration/Field/GetFields");

        handler.Requests.Single().RequestUri!.ToString().Should().StartWith("http://configuration/api/configuration/Field/GetFields");
    }

    [Theory]
    [InlineData("/api/base/Crud/GetList/x", true)]
    [InlineData("/api/configuration/Field/GetFields", false)]
    [InlineData("", false)]
    public void IsBaseUrl_ShouldDetectBaseUrls(string url, bool expected)
    {
        HttpClientsOptions.IsBaseUrl(url).Should().Be(expected);
    }
}