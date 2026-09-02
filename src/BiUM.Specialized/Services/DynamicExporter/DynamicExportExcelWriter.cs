using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace BiUM.Specialized.Services.DynamicExporter;

public static class DynamicExportExcelWriter
{
    public static byte[] WriteRows(IReadOnlyList<IDictionary<string, object?>> rows)
    {
        using var stream = new MemoryStream();
        using (var document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
        {
            var workbookPart = document.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();
            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            var sheetData = new SheetData();
            worksheetPart.Worksheet = new Worksheet(sheetData);
            var sheets = workbookPart.Workbook.AppendChild(new Sheets());
            sheets.Append(new Sheet
            {
                Id = workbookPart.GetIdOfPart(worksheetPart),
                SheetId = 1,
                Name = "Export"
            });

            if (rows.Count == 0)
            {
                document.Save();
                return stream.ToArray();
            }

            var columns = rows.SelectMany(r => r.Keys).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var headerRow = new Row();
            uint colIndex = 1;

            foreach (var column in columns)
            {
                headerRow.Append(CreateTextCell(GetColumnName(colIndex++), column));
            }

            sheetData.Append(headerRow);

            foreach (var row in rows)
            {
                var dataRow = new Row();
                colIndex = 1;

                foreach (var column in columns)
                {
                    row.TryGetValue(column, out var value);
                    dataRow.Append(CreateTextCell(GetColumnName(colIndex++), FormatCellValue(value)));
                }

                sheetData.Append(dataRow);
            }

            document.Save();
        }

        return stream.ToArray();
    }

    public static List<IDictionary<string, object?>> ExtractRowsFromApiResponse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.TryGetProperty("value", out var valueElement))
        {
            if (valueElement.ValueKind == JsonValueKind.Array)
            {
                return valueElement.EnumerateArray()
                    .Select(e => (IDictionary<string, object?>)ToDictionary(e))
                    .Where(x => x.Count > 0)
                    .ToList();
            }

            if (valueElement.ValueKind == JsonValueKind.Object)
            {
                var single = ToDictionary(valueElement);
                return single.Count > 0 ? new List<IDictionary<string, object?>> { single } : [];
            }
        }

        return [];
    }

    private static Dictionary<string, object?> ToDictionary(JsonElement element)
    {
        var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        if (element.ValueKind != JsonValueKind.Object)
        {
            return dict;
        }

        foreach (var property in element.EnumerateObject())
        {
            dict[property.Name] = property.Value.ValueKind switch
            {
                JsonValueKind.String => property.Value.GetString(),
                JsonValueKind.Number when property.Value.TryGetInt64(out var l) => l,
                JsonValueKind.Number => property.Value.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                _ => property.Value.GetRawText()
            };
        }

        return dict;
    }

    private static Cell CreateTextCell(string cellReference, string? text)
    {
        return new Cell
        {
            CellReference = cellReference,
            DataType = CellValues.String,
            CellValue = new CellValue(text ?? string.Empty)
        };
    }

    private static string FormatCellValue(object? value)
    {
        return value switch
        {
            null => string.Empty,
            IFormattable formattable => formattable.ToString(null, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
            _ => value.ToString() ?? string.Empty
        };
    }

    private static string GetColumnName(uint index)
    {
        var dividend = index;
        var columnName = string.Empty;

        while (dividend > 0)
        {
            var modulo = (dividend - 1) % 26;
            columnName = Convert.ToChar(65 + modulo) + columnName;
            dividend = (dividend - modulo) / 26;
        }

        return columnName;
    }
}