using System.Linq;

namespace BiUM.Contract.Models.Api;

public static class ApiResponseLogSummary
{
    public static string Format(ApiResponse? response)
    {
        if (response is null)
        {
            return "(null)";
        }

        if (response.Messages.Count == 0)
        {
            return "(no messages)";
        }

        return string.Join(
            "; ",
            response.Messages.Select(static m => $"{m.Severity}:{m.Code}:{m.Message}"));
    }
}
