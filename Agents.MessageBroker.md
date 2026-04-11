# Agents.MessageBroker.md — RabbitMQ istemcisi (BiUM)

Bu belge, **`IRabbitMQClient`** uygulamasının yayınlama tarafındaki davranışını ve bağlam / serileştirme iletişimini özetler (tüketici tarafı ayrıntıları kaynak dosyada).

## 1. Arayüz ve konum

- **`IRabbitMQClient`**: `BiUM.Core/MessageBroker/RabbitMQ/IRabbitMQClient.cs`
- Uygulama: **`RabbitMQClient`** — `BiUM.Infrastructure/Services/MessageBroker/RabbitMQ/RabbitMQClient.cs` (`internal sealed`)

Etkinlik: **`RabbitMQOptions.Enable`** false ise `PublishAsync` no-op döner.

## 2. Serileştirme ve içerik türü

- Olay gövdesi: **`IRabbitMQSerializer`** (MemoryPack tabanlı kullanım; içerik tipi varsayılan **`application/x-memorypack`**, kodlama **`brotli`**).
- **`IBaseEvent`** türevleri; `EventAttribute` ile exchange / yönlendirme bilgisi.

## 3. Yayın — exchange’li (komut benzeri)

- `EventAttribute.Exchange` doluysa: doğrudan exchange, `routingKey = {exchange}.{eventTypeSnake}`.
- **`BasicProperties`**: kalıcı mesaj, `MessageId`, AMQP `CorrelationId` = `CorrelationContext.CorrelationId`, timestamp.
- **Header’lar**:
  - **`x-correlation-context`**: serileştirilmiş `CorrelationContext` (serializer ile; HTTP’deki ile aynı erişim modeli)
  - **`x-bium-version`**, **`x-biapp-domain`** (`BiAppOptions.Domain`)

## 4. Bağlam

- **`ICorrelationContextAccessor`**: Yoksa `CorrelationContext.Empty` kullanılır.
- Ayrıntı: [Agents.CorrelationContext.md](Agents.CorrelationContext.md).

## 5. Dayanıklılık ve hata yönetimi

- Sabitler (sınıf içi): dead letter exchange **`common.dlx`**, retry header’ları (`x-retry-count`, `x-original-queue`, `x-failure-reason`, `x-failure-timestamp`).
- Tüketici kanalı, yeniden deneme ve DLX mantığı aynı dosyanın devamında tanımlıdır; değişiklik yaparken tam sınıfı okuyun.

## 6. Çekirdek olay türleri

- `BiUM.Core/MessageBroker/`: `IBaseEvent`, `BaseEvent`, `EventAttribute`, `TenantBaseEntityEvent`, örnek olaylar (`Events/` altı) — yeni olaylar burada tanımlanır.

## 7. AI ajanları için

Exchange adlandırma, header anahtarları (`HeaderKeys`), serializer veya DLX politikası değişirse bu dosya, `AGENTS.md` ve tüketiciler güncellenmelidir.
