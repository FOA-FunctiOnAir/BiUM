using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BiUM.Specialized.Services.DynamicApi;

public static partial class DynamicApiEntityCodegen
{
    [GeneratedRegex("[^a-zA-Z0-9_]", RegexOptions.None)]
    private static partial Regex NonIdentifierChars();

    public static string ToEntityTypeName(string schema, string tableName)
    {
        var schemaPart = SanitizeIdentifier(schema);
        var tablePart = SanitizeIdentifier(tableName);

        return $"DynamicEntity_{schemaPart}_{tablePart}";
    }

    public static string GenerateEntitySource(
        DynamicApiTableReference reference,
        IReadOnlyList<DynamicApiColumnMetadata> columns)
    {
        var typeName = ToEntityTypeName(reference.Schema, reference.TableName);
        var sb = new StringBuilder();

        sb.AppendLine("using System;");
        sb.AppendLine("using System.ComponentModel.DataAnnotations.Schema;");
        sb.AppendLine();
        sb.AppendLine("namespace BiUM.DynamicApi.Generated;");
        sb.AppendLine();
        sb.AppendLine($"[Table(\"{EscapeString(reference.TableName)}\", Schema = \"{EscapeString(reference.Schema)}\")]");
        sb.AppendLine($"public sealed class {typeName}");
        sb.AppendLine("{");

        if (columns.Count == 0)
        {
            sb.AppendLine("    public string? Placeholder { get; set; }");
        }
        else
        {
            foreach (var column in columns)
            {
                var propertyName = ToPropertyName(column.ColumnName);
                var clrType = column.ClrTypeName.Contains('.') ? column.ClrTypeName : column.ClrTypeName;
                var nullableSuffix = column.IsNullable && !clrType.EndsWith('?') && clrType is not "string" and not "byte[]"
                    ? "?"
                    : column.IsNullable && clrType is "string" ? "" : "";

                if (clrType is "string" or "byte[]" && column.IsNullable)
                {
                    nullableSuffix = "?";
                }

                sb.AppendLine($"    [Column(\"{EscapeString(column.ColumnName)}\")]");
                sb.AppendLine($"    public {clrType}{nullableSuffix} {propertyName} {{ get; set; }}");
            }
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    public static string GenerateDbContextSource(
        IReadOnlyList<(DynamicApiTableReference Reference, IReadOnlyList<DynamicApiColumnMetadata> Columns)> entities)
    {
        var sb = new StringBuilder();

        sb.AppendLine("using Microsoft.EntityFrameworkCore;");
        sb.AppendLine();
        sb.AppendLine("namespace BiUM.DynamicApi.Generated;");
        sb.AppendLine();
        sb.AppendLine("public sealed class DynamicTableDbContext : DbContext");
        sb.AppendLine("{");
        sb.AppendLine("    public DynamicTableDbContext(DbContextOptions<DynamicTableDbContext> options) : base(options) { }");
        sb.AppendLine();
        sb.AppendLine("    protected override void OnModelCreating(ModelBuilder modelBuilder)");
        sb.AppendLine("    {");

        foreach (var (reference, columns) in entities)
        {
            var typeName = ToEntityTypeName(reference.Schema, reference.TableName);
            sb.AppendLine($"        modelBuilder.Entity<{typeName}>().ToTable(\"{EscapeString(reference.TableName)}\", \"{EscapeString(reference.Schema)}\");");

            if (TryGetDeletedPropertyName(columns, out var deletedPropertyName, out var deletedClrTypeName))
            {
                sb.AppendLine($"        modelBuilder.Entity<{typeName}>().HasQueryFilter(e => {BuildDeletedFilterExpression(deletedPropertyName, deletedClrTypeName)});");
            }
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    public static bool TryGetDeletedPropertyName(
        IReadOnlyList<DynamicApiColumnMetadata> columns,
        out string propertyName,
        out string clrTypeName)
    {
        var deletedColumn = columns.FirstOrDefault(c =>
            string.Equals(c.ColumnName, "DELETED", StringComparison.OrdinalIgnoreCase));

        if (deletedColumn is null)
        {
            propertyName = string.Empty;
            clrTypeName = string.Empty;
            return false;
        }

        propertyName = ToPropertyName(deletedColumn.ColumnName);
        clrTypeName = deletedColumn.ClrTypeName;
        return true;
    }

    internal static string BuildDeletedFilterExpression(string propertyName, string clrTypeName) =>
        clrTypeName is "bool" or "bool?"
            ? $"!e.{propertyName}"
            : $"e.{propertyName} == 0";

    public static string GenerateDbContextFactorySource()
    {
        return """
            using BiUM.Specialized.Services.DynamicApi;
            using Microsoft.EntityFrameworkCore;

            namespace BiUM.DynamicApi.Generated;

            public static class DynamicTableDbContextFactory
            {
                public static DynamicTableDbContext Create(IDynamicApiExecutionContext ctx)
                {
                    var builder = new DbContextOptionsBuilder<DynamicTableDbContext>();
                    DynamicApiDbContextOptions.Configure(builder, ctx.DatabaseType, ctx.ConnectionString);
                    return new DynamicTableDbContext(builder.Options);
                }
            }
            """;
    }

    private static string SanitizeIdentifier(string value)
    {
        var sanitized = NonIdentifierChars().Replace(value, "_");

        if (sanitized.Length == 0)
        {
            return "Table";
        }

        if (char.IsDigit(sanitized[0]))
        {
            sanitized = "T_" + sanitized;
        }

        return sanitized;
    }

    private static string ToPropertyName(string columnName)
    {
        var parts = columnName.Split('_', StringSplitOptions.RemoveEmptyEntries);
        var sb = new StringBuilder();

        foreach (var part in parts)
        {
            if (part.Length == 0)
            {
                continue;
            }

            sb.Append(char.ToUpperInvariant(part[0]));

            if (part.Length > 1)
            {
                sb.Append(part[1..].ToLowerInvariant());
            }
        }

        if (sb.Length == 0)
        {
            return "Column";
        }

        return sb.ToString();
    }

    private static string EscapeString(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"");
}