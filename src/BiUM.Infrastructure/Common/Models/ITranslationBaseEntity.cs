namespace BiUM.Infrastructure.Common.Models;

public interface ITranslationBaseEntity : IEntity
{
    public Guid RecordId { get; set; }

    public string Column { get; set; }

    public Guid LanguageId { get; set; }

    public string? Translation { get; set; }
}