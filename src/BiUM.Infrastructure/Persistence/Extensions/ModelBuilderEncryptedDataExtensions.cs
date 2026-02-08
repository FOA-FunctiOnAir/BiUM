using System;
using System.Reflection;
using BiUM.Core.Common.Attributes;
using BiUM.Infrastructure.Persistence.ValueConverters;
using Microsoft.EntityFrameworkCore;

namespace BiUM.Infrastructure.Persistence.Extensions;

public static class ModelBuilderEncryptedDataExtensions
{
    public static ModelBuilder ApplyEncryptedDataConversion(this ModelBuilder modelBuilder, string encryptionKey)
    {
        if (string.IsNullOrEmpty(encryptionKey))
        {
            throw new ArgumentException("Encryption key is required for EncryptedData conversion.", nameof(encryptionKey));
        }

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;

            foreach (var property in entityType.GetProperties())
            {
                var memberInfo = property.PropertyInfo ?? (MemberInfo?)property.FieldInfo;

                if (memberInfo is null)
                {
                    continue;
                }

                var attr = memberInfo.GetCustomAttribute<EncryptedDataAttribute>();

                if (attr is null)
                {
                    continue;
                }

                if (property.ClrType != typeof(string))
                {
                    throw new InvalidOperationException(
                        $"EncryptedDataAttribute is only supported on string properties. Entity: {clrType.Name}, Property: {property.Name}.");
                }

                var converter = new EncryptedDataValueConverter(encryptionKey, attr.Reversible);

                _ = modelBuilder.Entity(clrType).Property(property.Name).HasConversion(converter);
            }
        }

        return modelBuilder;
    }
}