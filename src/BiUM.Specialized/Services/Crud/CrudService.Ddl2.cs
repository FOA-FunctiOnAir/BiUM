using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Data.Common;

namespace BiUM.Specialized.Services.Crud;

public partial class CrudService
{
    private async Task<int> ExecuteSqlAsync(string sql, IList<object?> parameters, CancellationToken ct)
    {
        var conn = _baseContext.Database.GetDbConnection();
        var shouldClose = false;

        if (conn.State != ConnectionState.Open)
        {
            await _baseContext.Database.OpenConnectionAsync(ct);
            shouldClose = true;
        }

        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;

            AddParams(cmd, parameters);

            var n = await cmd.ExecuteNonQueryAsync(ct);

            return n;
        }
        finally
        {
            if (shouldClose)
            {
                await _baseContext.Database.CloseConnectionAsync();
            }
        }
    }

    private async Task<long> QueryScalarLongAsync(string sql, object?[] parameters, CancellationToken ct)
    {
        var conn = _baseContext.Database.GetDbConnection();
        var shouldClose = false;

        if (conn.State != ConnectionState.Open)
        {
            await _baseContext.Database.OpenConnectionAsync(ct);
            shouldClose = true;
        }

        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;

            AddParams(cmd, parameters);

            var result = await cmd.ExecuteScalarAsync(ct);

            return result is null || result is DBNull ? 0 : Convert.ToInt64(result);
        }
        finally
        {
            if (shouldClose)
            {
                await _baseContext.Database.CloseConnectionAsync();
            }
        }
    }

    private async Task<IDictionary<string, object?>?> QuerySingleRowAsync(string sql, object?[] parameters, CancellationToken ct)
    {
        var list = await QueryRowsAsync(sql, parameters, ct);

        return list.FirstOrDefault();
    }

    private async Task<List<IDictionary<string, object?>>> QueryRowsAsync(string sql, object?[] parameters, CancellationToken ct)
    {
        var conn = _baseContext.Database.GetDbConnection();
        var shouldClose = false;

        if (conn.State != ConnectionState.Open)
        {
            await _baseContext.Database.OpenConnectionAsync(ct);
            shouldClose = true;
        }

        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;

            AddParams(cmd, parameters);

            var rows = new List<IDictionary<string, object?>>();

            using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

                for (var i = 0; i < reader.FieldCount; i++)
                {
                    var name = reader.GetName(i);
                    var val = await reader.IsDBNullAsync(i, ct) ? null : reader.GetValue(i);

                    dict[name] = val;
                }

                rows.Add(dict);
            }

            return rows;
        }
        finally
        {
            if (shouldClose)
            {
                await _baseContext.Database.CloseConnectionAsync();
            }
        }
    }

    private static void AddParams(DbCommand cmd, IList<object?> parameters)
    {
        for (var i = 0; i < parameters.Count; i++)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = $"@p{i}";
            p.Value = parameters[i] ?? DBNull.Value;

            cmd.Parameters.Add(p);
        }
    }

    private static void AddParams(DbCommand cmd, object?[] parameters) => AddParams(cmd, (IList<object?>)parameters.ToList());
}