using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace BiUM.Infrastructure.Common.Models;

[Table("__COMPENSATION_SNAPSHOT", Schema = "dbo")]
public class DomainCompensationSnapshot : TenantBaseEntity
{
    [Column("ENTITY_NAME")]
    public required string EntityName { get; set; }

    [Column("APPLICATION_ID")]
    public Guid? ApplicationId { get; set; }

    [Column("SNAPSHOT_TABLE_NAME")]
    public string? SnapshotTableName { get; set; }

    [Column("ENTITY_CLR_TYPE_NAME")]
    public string? EntityClrTypeName { get; set; }

    [Column("ENTITY_ID")]
    public Guid EntityId { get; set; }

    [Column("OPERATION_TYPE")]
    public int OperationType { get; set; }

    [Column("COMPENSATION_SESSION_ID")]
    public Guid CompensationSessionId { get; set; }

    [Column("OLD_DATA_JSON")]
    public string? OldDataJson { get; set; }

    [Column("NEW_DATA_JSON")]
    public string? NewDataJson { get; set; }

    [Column("VERSION")]
    public int Version { get; set; }

    [Column("STATE")]
    public int State { get; set; }

    [Column("EXPIRE_AT")]
    public DateTime? ExpireAt { get; set; }

    [Column("PROCESSED_AT")]
    public DateTime? ProcessedAt { get; set; }
}