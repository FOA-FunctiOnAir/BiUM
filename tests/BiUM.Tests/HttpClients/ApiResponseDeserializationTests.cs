using BiUM.Contract.Enums;
using BiUM.Contract.Models.Api;
using BiUM.Core.Serialization;
using BiUM.Infrastructure.Services.HttpClients;
using FluentAssertions;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Xunit;

namespace BiUM.Tests.HttpClients;

public sealed class ApiResponseDeserializationTests
{
    private const string ErrorBody = """{"messages":[{"code":"invalid_operation","message":"boom","severity":"error"}],"success":false}""";
    private const string SuccessBody = """{"pageNumber":2,"totalPages":5,"totalCount":42,"value":[1,2,3],"messages":[],"success":true}""";
    private const string PlainSuccessBody = """{"value":7,"messages":[],"success":true}""";

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions();

        BiJsonOptions.Configure(options);

        return options;
    }

    private static HttpClientService CreateService()
    {
        var service = (HttpClientService)RuntimeHelpers.GetUninitializedObject(typeof(HttpClientService));

        var field = typeof(HttpClientService).GetField("_jsonSerializerOptions", BindingFlags.Instance | BindingFlags.NonPublic)!;

        field.SetValue(service, CreateOptions());

        return service;
    }

    private static HttpResponseMessage CreateResponse(HttpStatusCode status, string body)
    {
        return new HttpResponseMessage(status)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
    }

    private static async Task<TResult> InvokeAsync<TResult>(string methodName, Type[] genericArguments, HttpResponseMessage response, bool isSuccessful)
    {
        var method = typeof(HttpClientService)
            .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(m => m.Name == methodName && m.GetGenericArguments().Length == genericArguments.Length);

        if (genericArguments.Length > 0)
        {
            method = method.MakeGenericMethod(genericArguments);
        }

        var task = method.Invoke(CreateService(), [response, isSuccessful, false, CancellationToken.None])!;

        var asTask = task.GetType().GetMethod("AsTask")!.Invoke(task, null)!;

        await (Task)asTask;

        return (TResult)asTask.GetType().GetProperty("Result")!.GetValue(asTask)!;
    }

    [Fact]
    public void Paginated_error_body_keeps_messages_and_is_not_successful()
    {
        var result = JsonSerializer.Deserialize<PaginatedApiResponse<int>>(ErrorBody, CreateOptions())!;

        result.Success.Should().BeFalse();
        result.Messages.Should().ContainSingle(m => m.Code == "invalid_operation" && m.Severity == MessageSeverity.Error);
        result.PageNumber.Should().Be(1);
        result.TotalCount.Should().Be(0);
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public void Paginated_success_body_round_trips_pagination_and_value()
    {
        var result = JsonSerializer.Deserialize<PaginatedApiResponse<int>>(SuccessBody, CreateOptions())!;

        result.Success.Should().BeTrue();
        result.PageNumber.Should().Be(2);
        result.TotalPages.Should().Be(5);
        result.TotalCount.Should().Be(42);
        result.Value.Should().Equal(1, 2, 3);
    }

    [Fact]
    public void Paginated_response_survives_serialize_then_deserialize()
    {
        var options = CreateOptions();

        var original = new PaginatedApiResponse<int>([1, 2], 25, 3, 10);

        original.AddMessage(new ResponseMessage { Code = "w", Message = "warn", Severity = MessageSeverity.Warning });

        var json = JsonSerializer.Serialize(original, options);

        var copy = JsonSerializer.Deserialize<PaginatedApiResponse<int>>(json, options)!;

        copy.PageNumber.Should().Be(3);
        copy.TotalPages.Should().Be(3);
        copy.TotalCount.Should().Be(25);
        copy.Value.Should().Equal(1, 2);
        copy.Messages.Should().ContainSingle(m => m.Code == "w");
    }

    [Fact]
    public async Task Paginated_non_success_status_with_error_body_returns_the_real_error()
    {
        using var response = CreateResponse(HttpStatusCode.BadRequest, ErrorBody);

        var result = await InvokeAsync<PaginatedApiResponse<int>>("TryDeserializePaginatedApiResponse", [typeof(int)], response, false);

        result.Success.Should().BeFalse();
        result.Messages.Should().ContainSingle(m => m.Code == "invalid_operation" && m.Message == "boom");
    }

    [Fact]
    public async Task Paginated_non_success_status_with_success_body_reports_unexpected_success()
    {
        using var response = CreateResponse(HttpStatusCode.BadRequest, SuccessBody);

        var result = await InvokeAsync<PaginatedApiResponse<int>>("TryDeserializePaginatedApiResponse", [typeof(int)], response, false);

        result.Success.Should().BeFalse();
        result.Messages.Should().ContainSingle(m => m.Code == "unexpected_success_response");
    }

    [Fact]
    public async Task Paginated_success_status_returns_body_as_is()
    {
        using var response = CreateResponse(HttpStatusCode.OK, SuccessBody);

        var result = await InvokeAsync<PaginatedApiResponse<int>>("TryDeserializePaginatedApiResponse", [typeof(int)], response, true);

        result.Success.Should().BeTrue();
        result.TotalCount.Should().Be(42);
        result.Value.Should().Equal(1, 2, 3);
    }

    [Fact]
    public async Task Generic_non_success_status_with_error_body_returns_the_real_error()
    {
        using var response = CreateResponse(HttpStatusCode.BadRequest, ErrorBody);

        var result = await InvokeAsync<ApiResponse<int>>("TryDeserializeApiResponse", [typeof(int)], response, false);

        result.Success.Should().BeFalse();
        result.Messages.Should().ContainSingle(m => m.Code == "invalid_operation" && m.Message == "boom");
    }

    [Fact]
    public async Task Generic_non_success_status_with_success_body_reports_unexpected_success()
    {
        using var response = CreateResponse(HttpStatusCode.BadRequest, PlainSuccessBody);

        var result = await InvokeAsync<ApiResponse<int>>("TryDeserializeApiResponse", [typeof(int)], response, false);

        result.Success.Should().BeFalse();
        result.Messages.Should().ContainSingle(m => m.Code == "unexpected_success_response");
    }

    [Fact]
    public async Task NonGeneric_non_success_status_with_error_body_returns_the_real_error()
    {
        using var response = CreateResponse(HttpStatusCode.BadRequest, ErrorBody);

        var result = await InvokeAsync<ApiResponse>("TryDeserializeApiResponse", [], response, false);

        result.Success.Should().BeFalse();
        result.Messages.Should().ContainSingle(m => m.Code == "invalid_operation" && m.Message == "boom");
    }
}
