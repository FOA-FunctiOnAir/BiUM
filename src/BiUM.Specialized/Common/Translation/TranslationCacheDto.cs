using System.Text.Json.Serialization;

namespace BiUM.Specialized.Common.Translation;

internal sealed class TranslationCacheDto
{
    [JsonPropertyName("t")]
    public required string Text { get; set; }
}