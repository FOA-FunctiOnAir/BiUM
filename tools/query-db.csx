#!/usr/bin/env dotnet-script
#r "nuget: Npgsql, 8.0.3"

// TEK DB SORGU:
//   dotnet-script query-db.csx "<conn>" @query.sql
//
// ÇAPRAZ DB KARŞILAŞTIRMA (diff modu):
//   dotnet-script query-db.csx "<conn1>" @q1.sql --diff "<conn2>" @q2.sql --key <kolon>
//   → q1 sonucunda olup q2 sonucunda olmayan satırları listeler
//
// Kurulum: dotnet tool install -g dotnet-script

using Npgsql;
using System.Text.RegularExpressions;

string ReadSqlArg(string arg)
{
    if (arg.StartsWith("@"))
    {
        var path = arg.Substring(1);
        if (!File.Exists(path)) { Console.Error.WriteLine($"HATA: Dosya bulunamadı: {path}"); Environment.Exit(1); }
        return File.ReadAllText(path).Trim();
    }
    return arg.Trim();
}

void AssertSelectOnly(string sql)
{
    if (!Regex.IsMatch(sql, @"^\s*SELECT\b", RegexOptions.IgnoreCase))
    {
        Console.Error.WriteLine("HATA: Yalnızca SELECT sorguları çalıştırılabilir.");
        Environment.Exit(1);
    }
    foreach (var kw in new[] { "INSERT", "UPDATE", "DELETE", "DROP", "CREATE", "ALTER", "TRUNCATE", "EXECUTE", "EXEC" })
        if (Regex.IsMatch(sql, $@"\b{kw}\b", RegexOptions.IgnoreCase))
        {
            Console.Error.WriteLine($"HATA: Yasak keyword '{kw}'.");
            Environment.Exit(1);
        }
}

async Task<List<Dictionary<string, string>>> RunQuery(string connStr, string sql)
{
    await using var conn = new NpgsqlConnection(connStr);
    await conn.OpenAsync();
    await using var cmd = new NpgsqlCommand(sql, conn);
    await using var reader = await cmd.ExecuteReaderAsync();
    var cols = Enumerable.Range(0, reader.FieldCount).Select(i => reader.GetName(i)).ToList();
    var rows = new List<Dictionary<string, string>>();
    while (await reader.ReadAsync())
        rows.Add(cols.ToDictionary(c => c, c => reader.IsDBNull(cols.IndexOf(c)) ? "NULL" : reader.GetValue(cols.IndexOf(c))?.ToString() ?? "NULL"));
    return rows;
}

void PrintTable(List<Dictionary<string, string>> rows)
{
    if (rows.Count == 0) { Console.WriteLine("(Sonuç yok)"); return; }
    var cols = rows[0].Keys.ToList();
    var widths = cols.ToDictionary(c => c, c => Math.Max(c.Length, rows.Max(r => r[c].Length)));
    string Fmt(Dictionary<string, string> row) => string.Join(" | ", cols.Select(c => row[c].PadRight(widths[c])));
    Console.WriteLine(string.Join(" | ", cols.Select(c => c.PadRight(widths[c]))));
    Console.WriteLine(string.Join("-+-", cols.Select(c => new string('-', widths[c]))));
    foreach (var row in rows) Console.WriteLine(Fmt(row));
    Console.WriteLine($"\n({rows.Count} kayıt)");
}

if (Args.Count < 2) { Console.Error.WriteLine("Kullanım: dotnet-script query-db.csx \"<conn>\" @query.sql [--diff \"<conn2>\" @q2.sql --key <kolon>]"); Environment.Exit(1); return; }

var conn1 = Args[0];
var sql1  = ReadSqlArg(Args[1]);
AssertSelectOnly(sql1);

// --diff modu
int diffIdx = Args.IndexOf("--diff");
if (diffIdx >= 0)
{
    if (Args.Count < diffIdx + 5 || Args[diffIdx + 3] != "--key")
    {
        Console.Error.WriteLine("--diff kullanımı: --diff \"<conn2>\" @q2.sql --key <kolon>");
        Environment.Exit(1); return;
    }
    var conn2   = Args[diffIdx + 1];
    var sql2    = ReadSqlArg(Args[diffIdx + 2]);
    var keyCol  = Args[diffIdx + 4];
    AssertSelectOnly(sql2);

    Console.WriteLine("Sorgu 1 çalışıyor...");
    var rows1 = await RunQuery(conn1, sql1);
    Console.WriteLine("Sorgu 2 çalışıyor...");
    var rows2 = await RunQuery(conn2, sql2);

    var lookup = rows2.Select(r => r.ContainsKey(keyCol) ? r[keyCol] : null)
                      .Where(v => v != null && v != "NULL")
                      .ToHashSet();

    var missing = rows1.Where(r => r.ContainsKey(keyCol) && !lookup.Contains(r[keyCol])).ToList();

    Console.WriteLine($"\n=== Sorgu 1'de olup Sorgu 2'de olmayan ({keyCol} üzerinden) ===");
    PrintTable(missing);
}
else
{
    var rows = await RunQuery(conn1, sql1);
    PrintTable(rows);
}
