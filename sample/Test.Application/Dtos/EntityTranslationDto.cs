using BiUM.Specialized.Common.Models;
using BiUM.Specialized.Mapping;
using BiUM.Test.Domain.Entities;

namespace BiUM.Test.Application.Dtos;

public class EntityTranslationDto : BaseTranslationDto, IMapFrom<CurrencyTranslation>
{
}
