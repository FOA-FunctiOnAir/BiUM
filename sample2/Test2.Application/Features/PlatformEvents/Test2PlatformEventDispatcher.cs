using BiUM.Core.PlatformIntegration;
using BiUM.Specialized.Services.PlatformIntegration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace BiApp.Test2.Application.Features.PlatformEvents;

public class Test2PlatformEventDispatcher : PlatformIntegrationEventDispatcher
{
    public Test2PlatformEventDispatcher(IEnumerable<IPlatformEventHandler> handlers, ILogger<Test2PlatformEventDispatcher> logger) : base(handlers, logger)
    {
    }
}