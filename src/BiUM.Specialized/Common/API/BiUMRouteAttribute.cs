using Microsoft.AspNetCore.Mvc;
using System;

namespace BiUM.Specialized.Common.API;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public class BiUMRouteAttribute : RouteAttribute
{
    public string DomainCode { get; }

    public BiUMRouteAttribute(string domainCode) : base($"/api/{domainCode}/[controller]/[action]")
    {
        DomainCode = domainCode;
    }
}

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public class BiUMBaseRouteAttribute : RouteAttribute
{
    public BiUMBaseRouteAttribute() : base("/api/base/[controller]/[action]")
    {
    }
}
