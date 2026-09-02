using System;

namespace BiUM.Specialized.Services.Crud;

public static class CrudSchemaHelper
{
    public static string ResolveSchema(Guid applicationId, Guid tenantId)
    {
        var applicationIdString = applicationId.ToString("N");
        var tenantIdString = tenantId.ToString("N");

        return $"t_{applicationIdString[..16]}_{tenantIdString[..16]}";
    }
}