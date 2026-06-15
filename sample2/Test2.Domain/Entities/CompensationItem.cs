using BiUM.Core.Compensation;
using BiUM.Infrastructure.Common.Models;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace BiApp.Test2.Domain.Entities;

[Table("COMPENSATION_ITEM", Schema = "dbo")]
public class CompensationItem : BaseEntity, ICompensation
{
    [Column("NAME")]
    public required string Name { get; set; }

    [Column("COMPENSATION_SESSION_ID")]
    public Guid? CompensationSessionId { get; set; }

    [Column("C_STATUS")]
    public string? CStatus { get; set; }
}