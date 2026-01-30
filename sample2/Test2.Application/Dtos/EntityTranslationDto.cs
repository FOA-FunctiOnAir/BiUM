using BiApp.Test2.Domain.Entities;
using BiUM.Specialized.Common.Models;
using BiUM.Specialized.Mapping;

namespace BiApp.Test2.Application.Dtos;

public class EntityTranslationDto : BaseTranslationDto, IMapFrom<AccountTranslation>
{
}