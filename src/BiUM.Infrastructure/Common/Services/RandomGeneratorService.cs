using BiUM.Core.Common.Configs;
using Microsoft.Extensions.Options;
using System;
using System.Linq;

namespace BiUM.Infrastructure.Common.Services;

public class RandomGeneratorService : IRandomGeneratorService
{
    private const string DefaultPassword = "123456";

    private readonly BiAppOptions _biAppOptions;

    public RandomGeneratorService(IOptions<BiAppOptions> biAppOptions)
    {
        _biAppOptions = biAppOptions.Value;
    }

    public string GetNewRandomPassword(string? defaultPassword, int length = 6)
    {
        if (_biAppOptions.Environment is "Development")
        {
            return !string.IsNullOrEmpty(defaultPassword) ? defaultPassword : DefaultPassword;
        }

        Random generator = new();

        int min = (int)Math.Pow(10, length - 1);
        int max = (int)Math.Pow(10, length) - 1;

        var r = generator.Next(min, max).ToString();

        if (r.Distinct().Count() == 1)
        {
            return GetNewRandomPassword(string.Empty, length);
        }

        return r;
    }
}