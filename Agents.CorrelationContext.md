# Agents.CorrelationContext.md — İstek bağlamı ve yayılım (BiUM)

Bu belge, **`CorrelationContext`** modelinin alanları, HTTP ve mesajlaşma üzerinden taşınması ve **`ICorrelationContextAccessor`** ile erişimi özetler.

## 1. Model

- **`BiUM.Contract/Models/CorrelationContext.cs`** — MemoryPack ile serileştirilebilir (`[MemoryPackable]`).
- Önemli alanlar: `CorrelationId` (zorunlu), `CompensationSessionId`, `ConnectionId`, `TraceId`, `IpAddress`, `ClientHost`, **`ApplicationId` (`Guid`, eksik bağlamda `Guid.Empty`)**, `TenantId`, `TenantName`, **`LanguageId` (`Guid`, platform varsayılanı `CorrelationContext.DefaultLanguageId` — `BiUM.Core` içindeki `Ids.Language.Turkish.Id` ile aynı değer)**, `ResourceId`, `User` (`UserContext`), `CreatedAt`.
- **`CorrelationContext.Empty`**: `CorrelationId` ve `ApplicationId` `Guid.Empty`, `LanguageId` = `DefaultLanguageId` (Türkçe); `SpecializedBase` istek bağlamı yoksa bunu kullanır.
- **`WithCompensationSessionId(Guid)`**: değişmez kopya üretir; telafi filtresi bunu kullanır.
- **Uyumluluk**: `ApplicationId` / `LanguageId` alanları `Guid?` iken üretilmiş **eski** `x-correlation-context` (MemoryPack) blob’ları yeni sürümle **deserialize edilemez**; tüm üreticilerin yeni pakete geçmesi gerekir.

## 2. HTTP header

- Anahtar: **`HeaderKeys.CorrelationContext`** → `x-correlation-context` (`BiUM.Core/Constants/HeaderKeys.cs`).
- **`CorrelationContextExtractorMiddleware`** (`BiUM.Infrastructure/Middlewares/CorrelationContextExtractorMiddleware.cs`): Base64 gövde → `ICorrelationContextSerializer.Deserialize` → `ICorrelationContextAccessor.CorrelationContext`; gelen ham header **`HttpContext.Items[CorrelationContextHttpItems.PassthroughHeader]`** içine yazılır (outbound passthrough için). Bozuk header’da log + bağlam atlanır.

### 2b. Gateway → istemci yanıt header’ları

- **`x-correlation-id`**: `CorrelationContextMiddleware` (BiApp.Gateway) yanıtta set eder; değer, gateway’de üretilen **`CorrelationId`** (Guid) ile aynıdır.
- **`HeaderKeys.TraceId`** (`x-trace-id`): Aynı middleware’de, OpenTelemetry **`Activity.TraceId`** (yoksa `HttpContext.TraceIdentifier`) ile set edilir; log/APM ile hizalama içindir.
- **`CorrelationId` yaşam döngüsü**: İlk atama gateway’de yapılır (`Activity.TraceId`’den Guid türetme veya yeni `Guid`). Değer, downstream isteklere **`x-correlation-context`** blob’u içinde taşınır. Mikroservisler bağlamı yalnızca **deserialize** eder; **`CorrelationId` yeniden üretilmez**. **`CompensatableApiActionFilter`** yalnızca **`CompensationSessionId`** ekler (`WithCompensationSessionId`); **`CorrelationId` değişmez**. Servisler arası HTTP’de **`HttpClientService`** mevcut accessor bağlamını aynı header ile iletir.

## 3. Serileştirme

- Arayüz: `ICorrelationContextSerializer` (`BiUM.Core/Serialization`).
- Uygulama: **`CorrelationContextSerializer`** — fiziksel dosya `BiUM.Infrastructure/Services/Serialization/CorrelationContextSerializer.cs`, **namespace** `BiUM.Specialized.Services.Serialization` (derleme biriminde Infrastructure projesinde).
- Biçim: MemoryPack + **Brotli** sıkıştırma (`MemoryPack.Compression`).

Outbound HTTP istemcisi aynı sözleşmeyi kullanır; bkz. [Agents.HttpClientService.md](Agents.HttpClientService.md) § Correlation.

## 4. Erişim (AsyncLocal)

- **`CorrelationContextAccessor`** (`BiUM.Infrastructure/Services/Authorization/CorrelationContextAccessor.cs`): `AsyncLocal` üzerinden istek içi bağlam; setter dolaylı tutamaç ile önceki bağlamı temizler.

## 4b. `ICorrelationContextProvider`

- **`CorrelationContextProvider`** (`BiUM.Infrastructure/Services/Authorization/CorrelationContextProvider.cs`): `Get()` önce **`ICorrelationContextAccessor`** üzerinden döner; accessor boşsa gelen istekteki `x-correlation-context` header’ından deserialize eder. İstek içinde accessor güncellenirse (ör. yeni telafi oturumu) `Get()` ile okunan değer stale kalmaz.

## 5. RabbitMQ

- **`RabbitMQClient`** yayınlarken `CorrelationContext`’i `IRabbitMQSerializer` ile serileştirir ve **`HeaderKeys.CorrelationContext`** altında binary header olarak ekler; ayrıca `CorrelationId` AMQP `CorrelationId` alanına yansır.
- Ayrıntı: [Agents.MessageBroker.md](Agents.MessageBroker.md).

## 6. Tenant ve CRUD

- Dinamik CRUD satır **`TENANT_ID`** doldurumu ve tanım düzlemi yetkileri `CorrelationContext.TenantId` ile ilişkilidir; bkz. [Agents.Crud.md](Agents.Crud.md).

## 6b. Kullanıcı saat dilimi (`TimeZoneId`)

- `CorrelationContext.TimeZoneId` (IANA, ör. `Europe/Istanbul`); Gateway web isteklerinde şimdilik sabit `Europe/Istanbul` doldurur. DB'de zamanlar UTC tutulur; dönüşüm yalnızca kullanıcıya giden/gelen değerlerde yapılır.
- Ortak yardımcı: `BiUM.Core.Common.Utils.UserTimeZone` — `Resolve(id)` (önbellekli; boş/geçersiz id → `null` = UTC), `ToUserTime(utc, zone)` (duvar saati, `Kind = Unspecified`), `ToUtc(userTime, zone)` (yaz saati boşluğundaki geçersiz saatler ileri kaydırılır), `ToUserOffset`. `Configure(accessor)` `UseSpecialized` içinde (`CorrelationContextTimeZone.Configure`) çağrılır.
- **`IDateTimeService`** (`BiUM.Infrastructure.Common.Services`): mevcut `Now` / `Today` / `TimeNow` / `Offset*` UTC'dir (audit ve sistem zamanları). Kullanıcı saati için: `UserTimeZone` (`TimeZoneInfo`, yoksa UTC), `UserNow`, `UserToday`, `ToUserTime(DateTime utc)`, `ToUserTime(DateTimeOffset)`, `ToUtc(DateTime userTime)` (ör. ekrandan gelen tarih filtresi).
- **`[UserTime]`** (`BiUM.Core.Common.Attributes`): UTC tutulan `DateTime` / `DateTime?` / `DateTimeOffset` / `DateTimeOffset?` alanı cevaba kullanıcının **ofsetsiz duvar saati** olarak yazılır (`"2026-10-25T18:44:12"`, web bunu olduğu gibi gösterir); istekte ofsetsiz değer kullanıcının saati sayılıp UTC'ye çevrilir, `Z`/ofsetli değer olduğu gibi an kabul edilir. `TimeZoneId` yoksa değer UTC (`…Z`) yazılır. Yalnızca işaretli alanları etkiler; ms'ler arası DTO'larda (ör. Configuration'dan dönen durum DTO'su) kullanılmaz.
- `BaseDto.CreatedAt` / `UpdatedAt` (`CorrelationContextTimeZone`, AutoMapper `AfterMap` ve `PopulateAuditFields`) aynı yardımcıyı kullanır.

## 7. AI ajanları için

Model alanı veya header adı değişirse Gateway ve tüm istemcilerle uyum için bu dosya, `HeaderKeys` ve [Agents.HttpClientService.md](Agents.HttpClientService.md) güncellenmelidir.
