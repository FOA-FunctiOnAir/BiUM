using BiUM.Specialized.Common.Models;
using BiUM.Specialized.Mapping;
using BiUM.Test2.Domain.Entities;

namespace BiUM.Test2.Application.Dtos;

public class EntityTranslationDto : BaseTranslationDto, IMapFrom<AccountTranslation>
{
}
