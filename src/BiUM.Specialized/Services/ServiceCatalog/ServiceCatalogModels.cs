using System.Collections.Generic;
using System.Text.Json;

namespace BiUM.Specialized.Services.ServiceCatalog;

public static class ServiceCatalogPropertySources
{
    public const string Query = "Query";

    public const string Body = "Body";

    public const string Route = "Route";

    public const string Form = "Form";

    public const string Response = "Response";
}

public static class ServiceCatalogClrTypes
{
    public const string Guid = "Guid";

    public const string String = "String";

    public const string Boolean = "Boolean";

    public const string Integer = "Integer";

    public const string Long = "Long";

    public const string Decimal = "Decimal";

    public const string Double = "Double";

    public const string DateTime = "DateTime";

    public const string DateOnly = "DateOnly";

    public const string TimeOnly = "TimeOnly";

    public const string Enum = "Enum";

    public const string Binary = "Binary";

    public const string File = "File";

    public const string Object = "Object";
}

public class ServiceCatalogEndpointDto
{
    public required string HttpMethod { get; set; }

    public required string Url { get; set; }

    public required string ControllerName { get; set; }

    public required string ActionName { get; set; }

    public string? Name { get; set; }

    public string? Description { get; set; }
}

public class ServiceCatalogPropertyDto
{
    public required string Name { get; set; }

    public required string Source { get; set; }

    public required string ClrType { get; set; }

    public string? EnumTypeName { get; set; }

    public bool IsArray { get; set; }

    public bool IsNullable { get; set; }

    public List<ServiceCatalogPropertyDto> Children { get; set; } = [];
}

public class ServiceCatalogManifestDto
{
    public required string Domain { get; set; }

    public required string HttpMethod { get; set; }

    public required string Url { get; set; }

    public required string ControllerName { get; set; }

    public required string ActionName { get; set; }

    public string? Name { get; set; }

    public string? Description { get; set; }

    public bool ResponseIsArray { get; set; }

    public bool ResponseIsPaginated { get; set; }

    public List<ServiceCatalogPropertyDto> Parameters { get; set; } = [];

    public List<ServiceCatalogPropertyDto> Response { get; set; } = [];
}

public class ServiceCatalogPreviewCommand
{
    public string? HttpMethod { get; set; }

    public string? Url { get; set; }
}

public class ServiceCatalogApplyCommand
{
    public string? HttpMethod { get; set; }

    public string? Url { get; set; }

    public JsonElement? Decisions { get; set; }
}