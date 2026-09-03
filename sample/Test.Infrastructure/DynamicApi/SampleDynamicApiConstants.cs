using System;

namespace BiApp.Test.Infrastructure.DynamicApi;

public static class SampleDynamicApiConstants
{
    public static readonly Guid ApplicationId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    public static readonly Guid MicroserviceId = Guid.Parse("bbbbbbbb-cccc-dddd-eeee-ffffffffffff");

    public static readonly Guid RuntimePlatformType = Guid.Parse("01a05cfc-0000-7000-8000-000000000001");

    public static readonly Guid CurrencyListApiId = Guid.Parse("cccccccc-dddd-eeee-ffff-111111111111");

    public static readonly Guid CurrencyListPagedApiId = Guid.Parse("dddddddd-eeee-ffff-1111-222222222222");

    public const string CurrencyListCode = "currency-list";

    public const string CurrencyListPagedCode = "currency-list-paged";
}