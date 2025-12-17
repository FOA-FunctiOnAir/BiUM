using BiUM.Infrastructure.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;

namespace BiUM.Infrastructure.Persistence.Configs;

public class TranslationBaseEntityConfig : IEntityTypeConfiguration<TranslationBaseEntity>
{
    public void Configure(EntityTypeBuilder<TranslationBaseEntity> b)
    {
        b.HasKey(x => x.Id);
        b.Property<Guid>(x => x.Id)
            .IsRequired()
            .HasField("ID")
            .HasColumnOrder(1);

        b.Property(e => e.Column)
            .IsRequired()
            .HasColumnName("COLUMN")
            .HasColumnOrder(2);

        b.Property(e => e.LanguageId)
            .IsRequired()
            .HasColumnName("LANGUAGE_ID")
            .HasColumnOrder(3);

        b.Property(e => e.Translation)
            .HasColumnName("TRANSLATION")
            .HasColumnOrder(3);
    }
}