using BiUM.Infrastructure.Common.Models;
using BiUM.Specialized.Mapping;

namespace BiUM.Specialized.Common.Models;

public class BaseEntityTranslationDto : BaseTranslationDto, IMapFrom<DomainCrudTranslation>;
