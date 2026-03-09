using BiUM.Core.Database;
using BiUM.Specialized.Common;
using System;

namespace BiUM.Specialized.Engine;

public abstract partial class BaseEngine : InfrastructureBase, IBaseRepository
{
    protected BaseEngine(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }
}