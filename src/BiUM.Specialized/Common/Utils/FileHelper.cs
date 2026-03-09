using BiUM.Contract.Enums;
using BiUM.Contract.Models.Api;
using System;
using System.IO;
using System.Reflection;

namespace BiUM.Specialized.Common.Utils;

// TODO: This will be converted to Services and injected where needed, but for now it's static for simplicity
public static class FileHelper
{
    public static ApiResponse<string> GetFileContent(Assembly assembly, string resourceName)
    {
        var response = new ApiResponse<string>();

        if (assembly is null)
        {
            response.AddMessage(new ResponseMessage()
            {
                Code = "assembly_is_null",
                Message = "Assembly is null",
                Severity = MessageSeverity.Error
            });

            return response;
        }

        if (string.IsNullOrEmpty(resourceName))
        {
            response.AddMessage(new ResponseMessage()
            {
                Code = "resourceName_is_null",
                Message = "ResourceName is null",
                Severity = MessageSeverity.Error
            });

            return response;
        }

        var streamResponse = GetStream(assembly, resourceName);

        if (!streamResponse.Success)
        {
            response.AddMessage(streamResponse.Messages);

            return response;
        }

        using var stream = streamResponse.Value!;
        using StreamReader streamReader = new(stream);

        response.Value = streamReader.ReadToEnd();

        return response;
    }

    private static ApiResponse<Stream> GetStream(Assembly assembly, string resourceName)
    {
        var response = new ApiResponse<Stream>();

        resourceName = resourceName.Replace(Path.PathSeparator, '.');

        var text = Array.Find(assembly.GetManifestResourceNames(), r => r.EndsWith(resourceName));

        if (string.IsNullOrEmpty(text))
        {
            response.AddMessage(new ResponseMessage()
            {
                Code = "embedded_resource_not_found",
                Message = $"There is no embedded resource that's name ends with {resourceName} in assembly {assembly.FullName}",
                Severity = MessageSeverity.Error
            });

            return response;
        }

        response.Value = assembly.GetManifestResourceStream(text);

        if (response.Value is null)
        {
            response.AddMessage(new ResponseMessage()
            {
                Code = "embedded_resource_cannot_be_loaded",
                Message = $"The embedded resource on path {text} cannot be loaded",
                Severity = MessageSeverity.Error
            });

            return response;
        }

        return response;
    }
}