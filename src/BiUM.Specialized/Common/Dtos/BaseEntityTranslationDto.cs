using BiUM.Infrastructure.Common.Models;
using BiUM.Specialized.Common.Models;
using BiUM.Specialized.Mapping;

namespace BiUM.Specialized.Common.Dtos;

public class BaseEntityTranslationDto : BaseTranslationDto, IMapFrom<DomainCrudTranslation>
{
}