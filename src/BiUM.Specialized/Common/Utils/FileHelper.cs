using BiUM.Core.Common.Enums;
using BiUM.Specialized.Common.API;
using System.Reflection;

namespace BiUM.Specialized.Common.Utils;

public static class FileHelper
{
    public static ApiResponse<string> GetFileContent(Assembly assembly, string resourceName)
    {
        var response = new ApiResponse<string>();

        if (assembly is null)
        {
            response.AddMessage("Assembly is null", MessageSeverity.Error);

            return response;
        }
        else if (string.IsNullOrEmpty(resourceName))
        {
            response.AddMessage("ResourceName is null", MessageSeverity.Error);

            return response;
        }

        var streamResponse = GetStream(assembly, resourceName);

        if (!streamResponse.Success)
        {
            response.AddMessage(streamResponse.Messages);

            return response;
        }

        using Stream stream = streamResponse.Value!;
        using StreamReader streamReader = new(stream);

        response.Value = streamReader.ReadToEnd();

        return response;
    }

    private static ApiResponse<Stream> GetStream(Assembly assembly, string resourceName)
    {
        var response = new ApiResponse<Stream>();

        resourceName = resourceName.Replace(Path.PathSeparator, '.');

        string text = Array.Find(assembly.GetManifestResourceNames(), (string r) => r.EndsWith(resourceName));

        if (string.IsNullOrEmpty(text))
        {
            response.AddMessage($"There is no embedded resource that's name ends with {resourceName} in assembly {assembly.FullName}", MessageSeverity.Error);

            return response;
        }

        response.Value = assembly.GetManifestResourceStream(text);

        if (response.Value is null)
        {
            response.AddMessage($"The embedded resource on path {text} cannot be loaded", MessageSeverity.Error);

            return response;
        }

        return response;
    }
}