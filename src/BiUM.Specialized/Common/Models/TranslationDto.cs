﻿namespace BiUM.Specialized.Common.Models;

public class TranslationDto
{
    public required Guid Id { get; set; }
    public required Guid LanguageId { get; set; }
    public string? Translation { get; set; }
    public int _rowStatus { get; set; }
}