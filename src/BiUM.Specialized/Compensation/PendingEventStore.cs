using BiUM.Core.Compensation;
using BiUM.Infrastructure.Common.Models;
using BiUM.Specialized.Database;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Compensation;

// IPendingEventStore'un IDbContext'e erişimi olan gerçek implementasyonu.
// RabbitMQClient (Infrastructure) bu tipi bilmez, sadece Core'daki arayüzü kullanır.
public sealed class PendingEventStore : IPendingEventStore
{
    private readonly IDbContext _dbContext;

    public PendingEventStore(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Guid compensationSessionId, string eventClrTypeName, byte[] payload, CancellationToken cancellationToken = default)
    {
        _dbContext.DomainPendingEvents.Add(new DomainPendingEvent
        {
            CompensationSessionId = compensationSessionId,
            EventClrTypeName = eventClrTypeName,
            Payload = payload
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}