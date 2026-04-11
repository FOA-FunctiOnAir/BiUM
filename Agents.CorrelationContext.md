# Agents.CorrelationContext.md — İstek bağlamı ve yayılım (BiUM)

Bu belge, **`CorrelationContext`** modelinin alanları, HTTP ve mesajlaşma üzerinden taşınması ve **`ICorrelationContextAccessor`** ile erişimi özetler.

## 1. Model

- **`BiUM.Contract/Models/CorrelationContext.cs`** — MemoryPack ile serileştirilebilir (`[MemoryPackable]`).
- Önemli alanlar: `CorrelationId` (zorunlu), `CompensationSessionId`, `ConnectionId`, `TraceId`, `IpAddress`, `ClientHost`, `ApplicationId`, `TenantId`, `TenantName`, `LanguageId`, `ResourceId`, `User` (`UserContext`), `CreatedAt`.
- **`WithCompensationSessionId(Guid)`**: değişmez kopya üretir; telafi filtresi bunu kullanır.

## 2. HTTP header

- Anahtar: **`HeaderKeys.CorrelationContext`** → `x-correlation-context` (`BiUM.Core/Constants/HeaderKeys.cs`).
- **`CorrelationContextExtractorMiddleware`** (`BiUM.Infrastructure/Middlewares/CorrelationContextExtractorMiddleware.cs`): Base64 gövde → `ICorrelationContextSerializer.Deserialize` → `ICorrelationContextAccessor.CorrelationContext`. Bozuk header’da log + bağlam atlanır.

## 3. Serileştirme

- Arayüz: `ICorrelationContextSerializer` (`BiUM.Core/Serialization`).
- Uygulama: **`CorrelationContextSerializer`** — fiziksel dosya `BiUM.Infrastructure/Services/Serialization/CorrelationContextSerializer.cs`, **namespace** `BiUM.Specialized.Services.Serialization` (derleme biriminde Infrastructure projesinde).
- Biçim: MemoryPack + **Brotli** sıkıştırma (`MemoryPack.Compression`).

Outbound HTTP istemcisi aynı sözleşmeyi kullanır; bkz. [Agents.HttpClientService.md](Agents.HttpClientService.md) § Correlation.

## 4. Erişim (AsyncLocal)

- **`CorrelationContextAccessor`** (`BiUM.Infrastructure/Services/Authorization/CorrelationContextAccessor.cs`): `AsyncLocal` üzerinden istek içi bağlam; setter dolaylı tutamaç ile önceki bağlamı temizler.

## 5. RabbitMQ

- **`RabbitMQClient`** yayınlarken `CorrelationContext`’i `IRabbitMQSerializer` ile serileştirir ve **`HeaderKeys.CorrelationContext`** altında binary header olarak ekler; ayrıca `CorrelationId` AMQP `CorrelationId` alanına yansır.
- Ayrıntı: [Agents.MessageBroker.md](Agents.MessageBroker.md).

## 6. Tenant ve CRUD

- Dinamik CRUD satır **`TENANT_ID`** doldurumu ve tanım düzlemi yetkileri `CorrelationContext.TenantId` ile ilişkilidir; bkz. [Agents.Crud.md](Agents.Crud.md).

## 7. AI ajanları için

Model alanı veya header adı değişirse Gateway ve tüm istemcilerle uyum için bu dosya, `HeaderKeys` ve [Agents.HttpClientService.md](Agents.HttpClientService.md) güncellenmelidir.
