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

    public string GetNewRandomPassword(string? defaultPassword)
    {
        if (_biAppOptions.Environment is "Development")
        {
            return !string.IsNullOrEmpty(defaultPassword) ? defaultPassword : DefaultPassword;
        }

        Random generator = new();

        var r = generator.Next(100001, 999999).ToString("D6");

        if (r.Distinct().Count() == 1)
        {
            r = GetNewRandomPassword(string.Empty);
        }

        return r;
    }
}