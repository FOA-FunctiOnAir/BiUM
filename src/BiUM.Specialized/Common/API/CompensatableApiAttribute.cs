using System;

namespace BiUM.Specialized.Common.API;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class CompensatableApiAttribute : Attribute;