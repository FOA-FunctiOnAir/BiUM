using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Core.Database.Native;

public interface INativeDatabase
{
    string Name { get; }

    Task<IReadOnlyList<TResult>> QueryAsync<TResult>(NativeSqlQuery<TResult> query, CancellationToken cancellationToken);

    Task<int> ExecuteAsync(NativeSqlCommand command, CancellationToken cancellationToken);

    Task<int> BulkMergeAsync<TRow>(NativeBulkMerge<TRow> merge, IReadOnlyCollection<TRow> rows, CancellationToken cancellationToken);

    Task<int> EnsureRangePartitionsAsync(NativeRangePartitionSet partitionSet, DateTime from, DateTime to, CancellationToken cancellationToken);

    Task<int> DropRangePartitionsBeforeAsync(NativeRangePartitionSet partitionSet, DateTime before, CancellationToken cancellationToken);
}