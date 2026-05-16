# Agents.MessageBroker.md — RabbitMQ istemcisi (BiUM)

Bu belge, **`IRabbitMQClient`** uygulamasının yayın–tüketim davranışını, **`RabbitMQClient`** (`BiUM.Infrastructure`) içindeki dallarla hizalı olarak özetler.

## 1. Arayüz ve konfigürasyon

- **`IRabbitMQClient`**: `BiUM.Core/MessageBroker/RabbitMQ/IRabbitMQClient.cs`
- Uygulama: **`RabbitMQClient`** — `BiUM.Infrastructure/Services/MessageBroker/RabbitMQ/RabbitMQClient.cs` (`internal sealed`)
- **Konfigürasyon**: Kök anahtar **`RabbitMQOptions`** (sınıf adı **`RabbitMqOptions`**). Zorunlu **`RabbitMQOptions:Default`** bölümü: `Enable` true ve `Hostname` dolu olmalı; BiUM başlangıçta doğrular. İkinci bir broker için `RabbitMQOptions:<ChildKey>` + `services.AddBiUMRabbitMqClients(configuration, "<ChildKey>")`; `Enable` false ise bu isim için DI kaydı yapılmaz. Tüketimde ana uygulama **`IRabbitMQClient`** (varsayılan), ek profil için **`[FromKeyedServices("<ChildKey>")] IRabbitMQClient`**.

Etkinlik: ilgili profilde **`RabbitMqOptions.Enable`** false ise `PublishAsync` no-op döner.

## 2. Serileştirme ve içerik türü

- Olay gövdesi: **`IRabbitMQSerializer`** (MemoryPack tabanlı kullanım; içerik tipi varsayılan **`application/x-memorypack`**, kodlama **`brotli`**).
- **`IBaseEvent`** türevleri; `EventAttribute` ile exchange / dallanma bilgisi.

## 3. Üç seçenek: `PublishAsync` + `StartConsumingAsync`

Üç seçenek, kodda sırayla **boş `Exchange`**, **dol `Exchange ≠ BiApp.Domain`**, **dol `Exchange = BiApp.Domain`** koşullarına karşılık gelir. **`BiAppOptions.Domain`** route kimliği ile aynı kabul edilir; **karşılaştırmalar** `ToLowerInvariant` / `OrdinalIgnoreCase` kullanır.

| Seçenek | Tip üzerinde `Event` | Yayın (`PublishAsync`, genel) | Tüketim (`StartConsumingAsync`) |
|--------|----------------------|-------------------------------|----------------------------------|
| **1** | **`[Event]`**, `Exchange` boş veya whitespace | **`fanout`** exchange **`{publisherDomain}.{event_snake}`** (`publisherDomain` = o anki `BiApp.Domain` küçültülmüş); prefix varsaysa uygulanır. | **Aynı fanout’a** bağlanır; kalıcı kuyruk **`{subscriberDomain}.{publisherIdentity}.{event_snake}`**. **Kendi yayınını dinleyen** serviste **`publisherIdentity` = `subscriberDomain`** (`test.test.event` gibi). Başka MS bu yayına girerken seçenek **2** kullanır. |
| **2** | **`[Event(Exchange = "x")]`, `x` ≠ çalışan `BiApp.Domain`** | Seçenek **1** veya yayın kodunun açtığı bir **fanout** ile uyum beklenir: dinleyicide **`x` küçük harfe normalize** (`x_normalized`). | **`fanout`** **`{x_normalized}.{event_snake}`**; kuyruk **`{subscriberDomain}.{x_normalized}.{event_snake}`**. Çok mikroservis aynı fanout’a ayrı kuyruklarla bağlanır → mesajdan **servis başına bir kuyruk kopyası**. |
| **3** | **`[Event(Exchange = "x")]`, `x` = çalışan `BiApp.Domain`** | **`direct`** exchange adı **`x`** (çoğunlukla küçültülmüş hub adı kullanılmalı); **`routingKey = {x}.{event_snake}`** (yayındaki ilk segment olarak `Exchange` dizgesi olduğu gibi kullanılabilir — tutarlılık için `x` sürekli küçük harf). | **`direct`** `subscriberDomain` exchange; **`routingKey = {subscriberDomain}.{event_snake}`**; kuyruk **`{subscriberDomain}.{event_snake}`**. Birçok yayımcı **aynı hub’a** yayınlar; tek (veya aynı kuyruğu paylaşan pod’lar arasında **yarışan**) tüketim bandı. |

**Kavram eşlemesi (eski 1→N / M→1 sözlüğü):** **Seçenek 1** yayını ≈ tek kimlikten yayın (**1→N** dinleyicide); **Seçenek 3** ≈ **çok yayımcı · tek mantıklı hub tüketicisi** (**M→1**); iki hub arası “yalnızca bir başka MS hedeflenir” sözleşmeleri yine hub üzerinden **Seçenek 3** ile yazılır. **Fanout’a geniş yayınlar** (**M→N** ama çekirdekte doğrudan hub değil) → yayın kodu fanout seçer (**Seçenek 2** ile eşlenen adres); örnek olarak **`CompensationSessionFinalizedEvent`**: `PublishAsync` bu tip için **`PublishCompensationSessionFinalizedFanoutAsync`** ile **Seçenek 3 doğrudan dalına hiç düşmez** (`Event` içinde `Exchange = "compensation"` yazsa bile).

> **Broker gerçekliği:** Çok mikroservisin “aynı olayı dinlemesi” AMQP dilinde çoğunlukla **aynı exchange’e bağlı ayrı kuyruklardan birer kopya** alınmasıdır; kodda bağ adları yukarıdaki kalıplara uygun tanımlanır.

### 3.1. Seçenek 1 — örnek

```csharp
[Event]
[MemoryPackable]
public partial class InvoiceGeneratedEvent : BaseEvent
{
    public required Guid InvoiceId { get; set; }
}
```

### 3.2. Seçenek 2 — örnek (başka domain fanout yayınına abone mikroservis)

```csharp
[Event(Exchange = "configuration")]
[MemoryPackable]
public partial class UserPermissionsChangedEvent : BaseEvent
{
    public Guid ApplicationId { get; set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
}
```

> **Uyum:** Hub **Seçenek 3 (direct)** ile yayınlanan bir ileti, fanout bekleyen Seçenek **2** tüketicisiyle **aynı fizik adresi paylaşmaz**. Yayın atan taraf multicast istiyorsa **Seçenek 1** (`[Event]` boş Configuration domain’inden) ile fanout’a basılmalıdır; sonra diğer servislerde Seçenek **2** tutarlı çalışır.

### 3.3. Seçenek 3 — örnekler (hub doğrudan; çok yayımcı → tek iş bandı veya iki uçlu sözleşme)

Çok mikroservis yayınlar, tek merkezi tüketiciler:

```csharp
[Event(Exchange = "audit")]
[MemoryPackable]
public partial class AuditLogEvent : BaseEvent
{
    public string ServiceName { get; set; } = string.Empty;
    public Guid UserId { get; set; }
}

[Event(Exchange = "observability")]
[MemoryPackable]
public partial class ServiceCalledEvent : BaseEvent
{
    public string? ServiceName { get; set; }
    public string? Endpoint { get; set; }
    public long ExecutionTimeMs { get; set; }
}
```

Hub’lı hedefe yönlendirmiş teslim:

```csharp
namespace BiApp.Orders.Application.Contract.Events;

[Event(Exchange = "fulfillment")]
[MemoryPackable]
public partial class ShipOrderRequestedEvent : BaseEvent
{
    public required Guid OrderId { get; set; }
}
```

Fulfillment mikroservisinde aynı tip (duplicate veya paylaşılan paket); **`BiApp.Domain` küçültülmüş olarak `fulfillment`** ile Seçenek **3** tüketicisi:

```csharp
namespace BiApp.Fulfillment.Application.External.Orders.Events;

[Event(Exchange = "fulfillment")]
[MemoryPackable]
public partial class ShipOrderRequestedEvent : BaseEvent
{
    public required Guid OrderId { get; set; }
}
```

Çok yayınlı geniş bildirim (örtük attribute; gerçek yol kodda fanout dallanması — `PublishAsync` özel işler):

```csharp
[Event(Exchange = "compensation")]
[MemoryPackable]
public partial class CompensationSessionFinalizedEvent : BaseEvent
{
    public required Guid CompensationSessionId { get; set; }
    public required bool Success { get; set; }
}
```

İş sırası ve yeniden teslim gereksinimleri DLX / retry parametreleriyle yönetilmelidir.

## 4. Yayın özeti (`PublishAsync`)

> Bölüm 3 seçenek tablosu bu sınıfla senkron tutulmalıdır; **özel tür dalları** güncellenirse burası ve birim testler güncellenir.

- **`CompensationSessionFinalizedEvent`**: genel doğrudan dala düşmez; **`PublishCompensationSessionFinalizedFanoutAsync`** (**fanout**).
- **`Exchange` boş (Seçenek 1 yayını):** **`fanout`** **`{publisherDomain}.{event_snake}`**.
- **`Exchange` dolu (Seçenek 3 doğrudan yol):** **`direct`** **`{exchange}`**, **`routingKey = {exchange}.{event_snake}`**.
- **`BasicProperties`**: kalıcı mesaj, `MessageId`, AMQP `CorrelationId` = `CorrelationContext.CorrelationId`, timestamp.
- **Header’lar:**
  - **`x-correlation-context`**: serileştirilmiş `CorrelationContext`.
  - **`x-bium-version`**, **`x-biapp-domain`** (`BiAppOptions.Domain`)

### 4.1. Hedef domain fanout yayını (`PublishToDomainAsync`)

Bazen çalışan süreç **`BiAppOptions.Domain`** dışında bir mikroservisin **kendi yayın kimliğiyle** yayın yapmalıdır (ör. Scheduler, hedef servisin `[Event]`’i boş olan olayına **aynı fanout adresini** yazmak için). **`PublishToDomainAsync(domain, message, …)`** şunu yapar:

- **`domain`**: **`Trim` + `ToLowerInvariant`**; `BiAppOptions.Domain` ile aynı sözleşme (çoğu serviste küçültülmış tek segment veya dotted çoklu segment).
- **Topoloji:** Seçenek **1** ile aynı: **`fanout`** exchange **`{domain}.{runtime_event_snake}`** (`message.GetType()`; türev sınıf adı).
- **`[Event]` `Exchange` dolu** olan türler için **`InvalidOperationException`** — bu yol **hub doğrudan (Seçenek 3)** veya **yabancı fanout (Seçenek 2)** mesajlarına uygulanmaz; **`PublishAsync`** kullanılmalıdır.
- **`CompensationSessionFinalizedEvent`**: **`PublishAsync`** ile aynı özel dallanma (**`PublishCompensationSessionFinalizedFanoutAsync`**); `domain` yok sayılır.
- **`BasicProperties`** ve header’lar (`x-biapp-domain` = gerçek yayıcı süreç) **`PublishAsync`** boş-exchange dalı ile uyumludur; yalnızca **exchange adı** hedef **`domain`** segmenti kullanılarak seçilir.

Hedef mikroserviste tüketim: ilgili event tipinde **`[Event]` boş**, **`StartConsumingAsync`** ile Seçenek **1** dinleyicisi (`{subscriberDomain}.{hedef_domain}.{event_snake}` kuyruk).

## 5. Bağlam

- **`ICorrelationContextAccessor`**: Yoksa `CorrelationContext.Empty` kullanılır.
- Ayrıntı: [Agents.CorrelationContext.md](Agents.CorrelationContext.md).

## 6. Dayanıklılık ve hata yönetimi

- Sabitler (sınıf içi): dead letter exchange **`common.dlx`**, retry header’ları (`x-retry-count`, `x-original-queue`, `x-failure-reason`, `x-failure-timestamp`).
- Tüketici kanalı, yeniden deneme ve DLX mantığı **`RabbitMQClient`** ile aynı dosyada; değişiklikte tam sınıfı okuyun.

## 7. Çekirdek olay türleri

- `BiUM.Core/MessageBroker/`: `IBaseEvent`, `BaseEvent`, `EventAttribute`, `TenantBaseEntityEvent`, örnek olaylar (`Events/` altı).

## 8. AI ajanları için

Exchange ve kuyruk ad kalıpları, `HeaderKeys`, serializer veya DLX politikası değişirse bu dosya, `AGENTS.md` ve kullanan mikroservisler güncellenmelidir.
