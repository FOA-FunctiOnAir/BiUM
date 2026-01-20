using BiApp.Test.Domain.Entities;
using BiUM.Specialized.Common.Models;
using BiUM.Specialized.Mapping;

namespace BiApp.Test.Application.Dtos;

public class EntityTranslationDto : BaseTranslationDto, IMapFrom<CurrencyTranslation>
{
}
