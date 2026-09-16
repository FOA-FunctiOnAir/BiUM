using System;

namespace BiApp.Test.Infrastructure.DynamicApi;

public static class SampleDynamicApiConstants
{
    public static readonly Guid ApplicationId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    public static readonly Guid MicroserviceId = Guid.Parse("bbbbbbbb-cccc-dddd-eeee-ffffffffffff");

    public static readonly Guid RuntimePlatformType = Guid.Parse("01a05cfc-0000-7000-8000-000000000001");

    public static readonly Guid CurrencyListApiId = Guid.Parse("cccccccc-dddd-eeee-ffff-111111111111");

    public static readonly Guid CurrencyListPagedApiId = Guid.Parse("dddddddd-eeee-ffff-1111-222222222222");

    public static readonly Guid CurrencyGetCurrenciesApiId = Guid.Parse("eeeeeeee-ffff-1111-2222-333333333333");

    public static readonly Guid CurrencyGetCurrencyApiId = Guid.Parse("ffffffff-1111-2222-3333-444444444444");

    public static readonly Guid MathMultiplyDivideGetApiId = Guid.Parse("12121212-3434-5656-7878-909090909090");

    public static readonly Guid MathMultiplyDividePostApiId = Guid.Parse("23232323-4545-6767-8989-a0a0a0a0a0a0");

    public const string CurrencyListCode = "currency-list";

    public const string CurrencyListPagedCode = "currency-list-paged";

    public const string CurrencyGetCurrenciesCode = "currency-get-currencies";

    public const string CurrencyGetCurrencyCode = "currency-get-currency";

    public const string MathMultiplyDivideGetCode = "math-multiply-divide-get";

    public const string MathMultiplyDividePostCode = "math-multiply-divide-post";

    public const string CurrencyListCallUrl = "/api/base/DynamicApi/Get/currency-list";

    public const string CurrencyListPagedCallUrl = "/api/base/DynamicApi/Get/currency-list-paged";

    public const string CurrencyGetCurrenciesCallUrl = "/api/base/DynamicApi/Get/currency-get-currencies";

    public const string CurrencyGetCurrencyCallUrl = "/api/base/DynamicApi/Get/currency-get-currency";

    public const string MathMultiplyDivideGetCallUrl = "/api/base/DynamicApi/Get/math-multiply-divide-get";

    public const string MathMultiplyDividePostCallUrl = "/api/base/DynamicApi/Post/math-multiply-divide-post";
}