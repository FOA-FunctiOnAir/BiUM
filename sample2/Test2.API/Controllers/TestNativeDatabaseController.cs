using BiApp.Test2.Infrastructure.Persistence.Native;
using BiUM.Core.Database.Native;
using BiUM.Specialized.Common.API;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BiApp.Test2.API.Controllers;

[BiUMRoute("test")]
public class TestNativeDatabaseController : ApiControllerBase
{
    private readonly INativeDatabase _metricsDatabase;

    public TestNativeDatabaseController([FromKeyedServices(SampleMetricsDatabase.Name)] INativeDatabase metricsDatabase)
    {
        _metricsDatabase = metricsDatabase;
    }

    [HttpPost]
    public async Task<IActionResult> EnsurePartitions([FromQuery] int daysAhead, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;

        var checkedCount = await _metricsDatabase.EnsureRangePartitionsAsync(SampleMetricsDatabase.Partitions, today.AddDays(-1), today.AddDays(Math.Clamp(daysAhead, 0, 30)), cancellationToken);

        return Ok(new { Checked = checkedCount });
    }

    [HttpPost]
    public async Task<IActionResult> WriteMetrics([FromQuery] int sourceId, [FromQuery] int count, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var end = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0, DateTimeKind.Utc);
        var rowCount = Math.Clamp(count, 1, 1440);

        var rows = Enumerable.Range(0, rowCount)
            .Select(index => new SampleMetricRow(
                sourceId,
                end.AddMinutes(-index),
                index * 1.5,
                [index, null, index * 10.0]))
            .ToList();

        await _metricsDatabase.EnsureRangePartitionsAsync(SampleMetricsDatabase.Partitions, rows[^1].MeasuredAt, end, cancellationToken);

        var affected = await _metricsDatabase.BulkMergeAsync(new SampleMetricMerge(), rows, cancellationToken);

        return Ok(new { Written = rows.Count, Affected = affected });
    }

    [HttpGet]
    public async Task<IActionResult> GetMetrics([FromQuery] int sourceId, [FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken cancellationToken)
    {
        var rows = await _metricsDatabase.QueryAsync(new SampleMetricRangeQuery(sourceId, from.ToUniversalTime(), to.ToUniversalTime()), cancellationToken);

        return Ok(rows);
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteMetrics([FromQuery] int sourceId, CancellationToken cancellationToken)
    {
        var deleted = await _metricsDatabase.ExecuteAsync(new DeleteSampleMetricsCommand(sourceId), cancellationToken);

        return Ok(new { Deleted = deleted });
    }
}