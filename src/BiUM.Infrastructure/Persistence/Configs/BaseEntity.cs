using BiUM.Infrastructure.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;

namespace BiUM.Infrastructure.Persistence.Configs;

public class BaseEntityConfig : IEntityTypeConfiguration<BaseEntity>
{
    public void Configure(EntityTypeBuilder<BaseEntity> b)
    {
        b.HasKey(x => x.Id);
        b.Property<Guid>(x => x.Id)
            .HasField("ID")
            .HasColumnOrder(1);

        b.Property(e => e.Active)
            .IsRequired()
            .HasColumnName("ACTIVE")
            .HasColumnOrder(2);

        b.Property(e => e.Created)
            .HasColumnName("CREATED")
            .HasColumnOrder(3);

        b.Property(e => e.CreatedTime)
            .HasColumnName("CREATED_TIME")
            .HasColumnOrder(4);

        b.Property(e => e.CreatedBy)
            .HasColumnName("CREATED_BY")
            .HasColumnOrder(5);

        b.Property(e => e.Updated)
            .HasColumnName("UPDATED")
            .HasColumnOrder(6);

        b.Property(e => e.UpdatedTime)
            .HasColumnName("UPDATED_TIME")
            .HasColumnOrder(7);

        b.Property(e => e.UpdatedBy)
            .HasColumnName("UPDATED_BY")
            .HasColumnOrder(8);

        b.Property(e => e.Test)
            .IsRequired()
            .HasDefaultValue(false)
            .HasColumnName("TEST")
            .HasColumnOrder(9);
    }
}
