using System;

namespace BiUM.Specialized.Common.Models;

public class BaseTranslationDto
{
    public Guid Id { get; set; }
    public Guid LanguageId { get; set; }
    public string? Translation { get; set; }
    public int _rowStatus { get; set; }
}
