using Microsoft.AspNetCore.Mvc;

namespace BiUM.Specialized.Common.API;

[AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
public class BiUMRouteAttribute(string domainCode) : RouteAttribute($"/api/{domainCode}/[controller]/[action]");

[AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
public class BiUMBaseRouteAttribute() : RouteAttribute($"/api/base/[controller]/[action]");