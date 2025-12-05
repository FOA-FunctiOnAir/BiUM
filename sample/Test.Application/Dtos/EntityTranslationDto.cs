using BiUM.Test.Domain.Entities;
using BiUM.Specialized.Common.Models;
using BiUM.Specialized.Mapping;

namespace BiUM.Test.Application.Dtos;

public class EntityTranslationDto : BaseTranslationDto, IMapFrom<CurrencyTranslation>
{
}