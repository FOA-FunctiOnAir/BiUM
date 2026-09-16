using AutoMapper;
using BiUM.Contract.Models;
using BiUM.Core.Authorization;
using BiUM.Core.Common.Configs;
using BiUM.Specialized.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;

namespace BiUM.Specialized.Common;

public abstract partial class SpecializedBase
{
    private readonly ICorrelationContextProvider _correlationContextProvider;

    protected CorrelationContext CorrelationContext => _correlationContextProvider.Get() ?? CorrelationContext.Empty;
    protected ITranslationService TranslationService { get; }
    protected ILogger<SpecializedBase> Logger { get; }
    protected IMapper Mapper { get; }

    protected BiAppOptions BiAppOptions { get; }

    protected SpecializedBase(IServiceProvider serviceProvider)
    {
        _correlationContextProvider = serviceProvider.GetRequiredService<ICorrelationContextProvider>();
        TranslationService = serviceProvider.GetRequiredService<ITranslationService>();
        Logger = serviceProvider.GetRequiredService<ILogger<SpecializedBase>>();
        Mapper = serviceProvider.GetRequiredService<IMapper>();
        BiAppOptions = serviceProvider.GetRequiredService<IOptions<BiAppOptions>>().Value;
    }
}