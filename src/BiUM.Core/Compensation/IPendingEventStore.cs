using System;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Core.Compensation;

// Aktif bir compensation session altında ertelenen (henüz publish edilmemiş) event'lerin
// kalıcı deposu. RabbitMQClient (Infrastructure) bu arayüzü DB'ye dokunmadan kullanır;
// gerçek implementasyon (IDbContext'e erişimi olan) BiUM.Specialized'da yaşar.
public interface IPendingEventStore
{
    Task AddAsync(Guid compensationSessionId, string eventClrTypeName, byte[] payload, CancellationToken cancellationToken = default);
}