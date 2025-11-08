using BiUM.Specialized.Common.MediatR;

namespace BiUM.Specialized.Common.Translation;

public record SaveDomainTranslationCommand : BaseCommandDto
{
    public Guid ApplicationId { get; set; }
    public string Code { get; set; }
    public IList<SaveDomainTranslationCommandDetail>? Translations { get; set; }
}

public record UpdateBoltDomainTranslationCommand : BaseCommandDto
{
    public Guid MicroserviceId { get; set; }
}

public record DeleteDomainTranslationCommand : BaseCommandDto
{
    public Guid MicroserviceId { get; set; }
}

public class SaveDomainTranslationCommandDetail
{
    public Guid Id { get; set; }
    public Guid LanguageId { get; set; }
    public string Text { get; set; }
    public int _rowStatus { get; set; }
}