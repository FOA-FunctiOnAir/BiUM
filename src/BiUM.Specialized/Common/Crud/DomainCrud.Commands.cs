using BiUM.Specialized.Common.Dtos;
using BiUM.Specialized.Common.MediatR;
using System;
using System.Collections.Generic;

namespace BiUM.Specialized.Common.Crud;

public record SaveDomainCrudCommand : BaseCommandDto
{
    public Guid MicroserviceId { get; set; }
    public IReadOnlyList<BaseEntityTranslationDto>? NameTr { get; set; }
    public string? Code { get; set; }
    public string? TableName { get; set; }
    public IList<SaveDomainCrudCommandColumn>? DomainCrudColumns { get; set; }
}

public record PublishDomainCrudCommand : BaseCommandDto
{
}

public record DeleteDomainCrudCommand : BaseCommandDto
{
}

public class SaveDomainCrudCommandColumn
{
    public Guid Id { get; set; }
    public Guid CrudId { get; set; }
    public string? PropertyName { get; set; }
    public string? ColumnName { get; set; }
    public Guid FieldId { get; set; }
    public Guid DataTypeId { get; set; }
    public int? MaxLength { get; set; }
    public int SortOrder { get; set; }
    public int _rowStatus { get; set; }
}