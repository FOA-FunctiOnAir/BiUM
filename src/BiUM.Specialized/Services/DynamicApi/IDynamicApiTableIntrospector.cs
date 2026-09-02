using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Services.DynamicApi;

public interface IDynamicApiTableIntrospector
{
    Task<IReadOnlyList<DynamicApiTableReference>> ListCatalogTablesAsync(CancellationToken cancellationToken);

    Task<bool> TableExistsAsync(string schema, string tableName, CancellationToken cancellationToken);

    Task<IReadOnlyList<DynamicApiColumnMetadata>> GetColumnsAsync(
        string schema,
        string tableName,
        CancellationToken cancellationToken);
}