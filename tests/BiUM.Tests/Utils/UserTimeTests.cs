using BiUM.Contract.Models;
using BiUM.Core.Common.Attributes;
using BiUM.Core.Common.Utils;
using BiUM.Infrastructure.Common.Services;
using BiUM.Infrastructure.Services.Authorization;
using BiUM.Specialized.Services.ServiceCatalog;
using FluentAssertions;
using System.Text.Json;
using Xunit;

namespace BiUM.Tests.Utils;

public sealed class UserTimeTests
{
    private sealed class SampleDto
    {
        [UserTime]
        public DateTime? ShippedAt { get; set; }

        [UserTime]
        public DateTimeOffset ConfirmedAt { get; set; }

        public DateTime RawAt { get; set; }
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly DateTime Utc1544 = new(2026, 10, 25, 15, 44, 12, DateTimeKind.Utc);

    private static CorrelationContextAccessor UseTimeZone(string? timeZoneId)
    {
        var accessor = new CorrelationContextAccessor
        {
            CorrelationContext = new CorrelationContext { CorrelationId = Guid.NewGuid(), TimeZoneId = timeZoneId }
        };

        UserTimeZone.Configure(accessor);

        return accessor;
    }

    [Theory]
    [InlineData("Europe/Istanbul", true)]
    [InlineData(" europe/istanbul ", true)]
    [InlineData("Not/AZone", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Resolve_ShouldFindKnownZones_AndReturnNullOtherwise(string? timeZoneId, bool found)
    {
        (UserTimeZone.Resolve(timeZoneId) is not null).Should().Be(found);
    }

    [Fact]
    public void ToUserTime_And_ToUtc_ShouldRoundTrip()
    {
        var istanbul = UserTimeZone.Resolve("Europe/Istanbul");

        var local = UserTimeZone.ToUserTime(Utc1544, istanbul);

        local.Should().Be(new DateTime(2026, 10, 25, 18, 44, 12));
        local.Kind.Should().Be(DateTimeKind.Unspecified);
        UserTimeZone.ToUtc(local, istanbul).Should().Be(Utc1544);
        UserTimeZone.ToUtc(local, istanbul).Kind.Should().Be(DateTimeKind.Utc);
        UserTimeZone.ToUserTime(Utc1544, null).Should().Be(Utc1544);
    }

    [Fact]
    public void Conversions_ShouldFollowDaylightSavingAndSkipInvalidLocalTimes()
    {
        var berlin = UserTimeZone.Resolve("Europe/Berlin");

        UserTimeZone.ToUserTime(new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc), berlin).Hour.Should().Be(13);
        UserTimeZone.ToUserTime(new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc), berlin).Hour.Should().Be(14);
        UserTimeZone.ToUtc(new DateTime(2026, 3, 29, 2, 30, 0), berlin).Should().Be(new DateTime(2026, 3, 29, 1, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void DateTimeService_ShouldUseCorrelationTimeZone_AndFallBackToUtc()
    {
        var service = new DateTimeService(UseTimeZone("Europe/Istanbul"));

        service.UserTimeZone.Id.Should().Contain("Istanbul");
        service.ToUserTime(Utc1544).Should().Be(new DateTime(2026, 10, 25, 18, 44, 12));
        service.ToUserTime(new DateTimeOffset(Utc1544)).Should().Be(new DateTime(2026, 10, 25, 18, 44, 12));
        service.ToUtc(new DateTime(2026, 10, 25, 18, 44, 12)).Should().Be(Utc1544);
        (service.UserNow - DateTime.UtcNow).TotalHours.Should().BeApproximately(3, 0.01);

        var withoutZone = new DateTimeService(UseTimeZone(null));

        withoutZone.UserTimeZone.Should().Be(TimeZoneInfo.Utc);
        withoutZone.ToUserTime(Utc1544).Should().Be(Utc1544);
        new DateTimeService().Now.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void UserTimeAttribute_ShouldWriteUserWallClock_AndReadBackAsUtc()
    {
        UseTimeZone("Europe/Istanbul");

        var json = JsonSerializer.Serialize(new SampleDto { ShippedAt = Utc1544, ConfirmedAt = new DateTimeOffset(Utc1544), RawAt = Utc1544 }, JsonOptions);

        json.Should().Contain("\"shippedAt\":\"2026-10-25T18:44:12\"");
        json.Should().Contain("\"confirmedAt\":\"2026-10-25T18:44:12\"");
        json.Should().Contain("\"rawAt\":\"2026-10-25T15:44:12Z\"");

        var fromWallClock = JsonSerializer.Deserialize<SampleDto>("{\"shippedAt\":\"2026-10-25T18:44:12\",\"confirmedAt\":\"2026-10-25T18:44:12\"}", JsonOptions)!;

        fromWallClock.ShippedAt.Should().Be(Utc1544);
        fromWallClock.ShippedAt!.Value.Kind.Should().Be(DateTimeKind.Utc);
        fromWallClock.ConfirmedAt.UtcDateTime.Should().Be(Utc1544);

        var fromInstant = JsonSerializer.Deserialize<SampleDto>("{\"shippedAt\":\"2026-10-25T15:44:12Z\",\"confirmedAt\":\"2026-10-25T18:44:12+03:00\"}", JsonOptions)!;

        fromInstant.ShippedAt.Should().Be(Utc1544);
        fromInstant.ConfirmedAt.UtcDateTime.Should().Be(Utc1544);

        JsonSerializer.Deserialize<SampleDto>("{\"shippedAt\":null}", JsonOptions)!.ShippedAt.Should().BeNull();
    }

    [Fact]
    public void UserTimeAttribute_ShouldKeepUtc_WhenUserHasNoTimeZone()
    {
        UseTimeZone(null);

        JsonSerializer.Serialize(new SampleDto { ShippedAt = Utc1544 }, JsonOptions).Should().Contain("\"shippedAt\":\"2026-10-25T15:44:12.0000000Z\"");
    }

    [Fact]
    public void ServiceCatalogEndpoint_ShouldReturnPreviewRequestedAtInUserTime()
    {
        UseTimeZone("Europe/Istanbul");

        var json = JsonSerializer.Serialize(new ServiceCatalogEndpointDto
        {
            HttpMethod = "GET",
            Url = "/api/x",
            ControllerName = "X",
            ActionName = "Get",
            PreviewRequestedAt = Utc1544
        }, JsonOptions);

        json.Should().Contain("\"previewRequestedAt\":\"2026-10-25T18:44:12\"");
    }
}