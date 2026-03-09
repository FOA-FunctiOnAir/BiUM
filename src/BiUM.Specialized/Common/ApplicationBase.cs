using System;

namespace BiUM.Specialized.Common;

public abstract partial class ApplicationBase : SpecializedBase
{
    protected ApplicationBase(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }
}