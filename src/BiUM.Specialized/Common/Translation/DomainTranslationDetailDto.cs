using BiUM.Infrastructure.Common.Models;
using BiUM.Specialized.Common.Models;
using BiUM.Specialized.Mapping;

namespace BiUM.Specialized.Common.Translation;

public class DomainTranslationDetailDto : BaseDto, IMapFrom<DomainTranslationDetail>
{
    public Guid LanguageId { get; set; }
    public string Text { get; set; }
}