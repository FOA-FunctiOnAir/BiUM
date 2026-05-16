# Agents.Redis.md — Redis önbellek istemcisi (BiUM)

Bu belge, **`IRedisClient`** kaydı ve **`RedisOptions`** yapılandırmasını **`RedisServiceCollectionExtensions`** ve **`RedisClient`** ile hizalı özetler.

## 1. Arayüz ve uygulama

- **`IRedisClient`**: `BiUM.Core/Caching/Redis/IRedisClient.cs` — anahtar tabanlı `Get`/`Add`/`Replace`/`Remove`, koşullu silme/değiştirme (Lua), süre bilgisi.
- Uygulama: **`RedisClient`** — `BiUM.Infrastructure/Services/Caching/Redis/RedisClient.cs` (StackExchange.Redis `IConnectionMultiplexer` / `IDatabase`).
- DTO tipleri: `BiUM.Contract` altında `CacheItem<T>` vb.

## 2. Konfigürasyon (`RedisOptions`)

Sınıf: `BiUM.Core/Common/Configs/RedisOptions.cs`

| Üye | Açıklama |
|-----|-----------|
| **`Name`** | Sabit yapı kök anahtarı: **`"RedisOptions"`** (konfigürasyon bölüm adı ile aynı). |
| **`DefaultClientKey`** | Varsayılan profil anahtarı: **`"Default"`** → **`RedisOptions:Default`**. |
| **`Enable`** | **false** ise ilgili profilde **`RedisClient` oluşturulmaz**; DI’da **`IRedisClient` kaydı da yapılmaz** (çoğaltılmış çağrılarda bile). |
| **`ConnectionString`** | StackExchange.Redis bağlantı dizesi. |
| **`DefaultCacheTimeout`** | Opsiyonel varsayılan TTL; kullanıcı API’sinde parametre ile geçişte kullanılabilir. |

## 3. DI kaydı (`AddBiUMRedisClients`)

Kaynak: `BiUM.Infrastructure/Extensions/RedisServiceCollectionExtensions.cs` (namespace **`Microsoft.Extensions.DependencyInjection`** ile `IServiceCollection` uzantıları).

- **`services.AddBiUMRedisClients(configuration)`**
  - `Configure<RedisOptions>("Default", configuration.GetSection("RedisOptions:Default"))` benzeri: **`RedisOptions:Default`** bölümünü bağlar.
  - Sadece **`RedisOptions:Default:Enable`** **true** ise **tek**, **isimlendirilmemiş** **`IRedisClient`** singleton kaydedilir (`Default` anahtarı ile `RedisClient`).
  - Aynı uzantı tekrar çağrılırsa ve zaten kayıtlı **`IRedisClient`** varsa **ikinci kayıt eklenmez** (çoğaltma korunur).

- **`services.AddBiUMRedisClients(configuration, "<ChildKey>")`**
  - Önce varsayılanı yukarıdaki gibi dener (`Default` için `Enable` false ise varsayılan yine oluşmaz).
  - **`ChildKey`** **`RedisOptions`** altında ikinci bir bölüm: **`RedisOptions:<ChildKey>`**.
  - **`ChildKey`** `DefaultClientKey` (`"Default"`) ile **aynı olamaz**; aksi halde **`ArgumentException`** (varsayılan için tek argümanlı overload kullanılmalı).
  - **`RedisOptions:<ChildKey>:Enable`** **true** ise **`AddKeyedSingleton<IRedisClient>(<ChildKey>)`** ile **`RedisClient`** eklenir; aynı anahtar zaten kayıtlıysa **atlanır**.
  - Tüketim: **`[FromKeyedServices("<ChildKey>")] IRedisClient`**.

Her profil için `RedisClient` oluşturucusu `IOptionsMonitor<RedisOptions>` üzerinden **o profilin adını** (`optionsName`) kullanarak ilgili bölümü okur.

## 4. Etkisiz yapılandırma

Profilde **`Enable: false`** ise:

- `IRedisClient` **o profil için DI’ya konmaz**.
- Varsayılan `AddBiUMRedisClients` çağrısında `Enable` false ise uygulama **`IRedisClient`** olmadan da ayağa kalkabilir; Redis kullanan kodların bu durumu göz önünde bulundurması gerekir (veya özellik açılana kadar Redis çağrısı yapmaması).

## 5. AI ajanları için

`RedisOptions` alanları, `RedisServiceCollectionExtensions` dal koşulları veya `RedisClient` API’si değişirse bu dosya ile kök **`AGENTS.md`** senkron tutulmalıdır.
