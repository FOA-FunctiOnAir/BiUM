﻿using BiUM.Infrastructure.Common.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace BiUM.Bolt.Database.Entities;

[Table("__BOLT_STATUS", Schema = "dbo")]
public class BoltStatus : BaseEntity
{
    [Column("LAST_TRANSACTION_ID")]
    public Guid? LastTransactionId { get; set; }

    [Column("ERROR")]
    public string? Error { get; set; }
}