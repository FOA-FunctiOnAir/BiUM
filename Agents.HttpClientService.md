# Agents.HttpClientService.md — Mikroservis HTTP istemcisi (BiUM)

Bu belge, FOA servislerinin birbirini **HTTP** üzerinden çağırması için BiUM’daki **`IHttpClientsService`** / **`HttpClientService`** katmanını özetler.

## 1. Arayüz ve kayıt

| Öğe | Konum |
|-----|--------|
| Sözleşme | `BiUM.Core/HttpClients/IHttpClientsService.cs` |
| Uygulama | `BiUM.Infrastructure/Services/HttpClients/HttpClientService.cs` |
| DI | `BiUM.Specialized/ApplicationExtensions.Services.cs` — `AddScoped<IHttpClientsService, HttpClientService>()` |
| Fabrika | `BiUM.Infrastructure/ApplicationExtensions.Services.cs` — `AddHttpClient()`, `HttpClientsOptions` yapılandırması |

## 2. Yapılandırma (`HttpClientsOptions`)

- Bölüm adı: `HttpClientsOptions` (`BiUM.Core/Common/Configs/HttpClientsOptions.cs`).
- **`BaseUrl`**, **`Environment`**, isteğe bağlı **`Domains`**: `/api/{servisAnahtarı}/...` biçimindeki iç URL’ler, `Domains` sözlüğüyle gerçek taban adreslere çözülür.
- İç servis tipleri **Crud** / **DynamicApi** için `GetFullUrl(microserviceRootPath, url)` ile mikroservis kök yolu + `base` anahtarı kullanılır; diğer iç çağrılarda `GetFullUrl(url)` yeterlidir.
- Eşleşmeyen servis anahtarı: `InvalidOperationException`.

## 3. API yüzeyi

- **`CallService` / `CallService<TResponse>(Guid serviceId, ...)`**  
  Önce Configuration üzerinden servis tanımı alınır: `GET .../api/configuration/Service/GetService?Id=...` (`GetServiceInfoAsync`).  Ardından **`ServiceDto`**: iç mi dış mı (`Ids.Parameter.ServiceType`), HTTP metodu (`Ids.Parameter.HttpType`), URL, kimlik doğrulama, zaman aşımı ile istek yürütülür.
- **`Get` / `Post` / `Post<TResponse>(string url, ...)`**  
  Doğrudan URL; `external` bayrağı yanıtın `ApiResponse` JSON’u mu yoksa ham T mi olduğunu ayırt etmek için kullanılır.

**Ortak davranış**

- Varsayılan HTTP zaman aşımı: 5 dakika; servis kaydındaki `TimeoutMs` varsa named client için o kullanılır.
- **`IHttpClientFactory`**: named client anahtarı iç çağrılarda çoğunlukla `service.Id`; URL tabanlı çağrılarda `host:port` veya path segmenti türetilir.
- İstek gövdesi JSON (`application/json`); GET/DELETE tarafında parametreler sorgu string’ine dönüşür; `Q`, `PageStart`, `PageSize` desteği.
- Dış servis kimlik doğrulama türleri: NoAuth, Basic, BearerStatic, ApiKeyHeader, ApiKeyQuery, CustomHeader (JSON header sözlüğü), OAuth2 client credentials, OAuth2 password (token uçları ayrı HTTP çağrıları).

## 4. Correlation (bağlam) yayılımı

`TryAddCorrelationContext`:

1. Gelen HTTP isteğinde `CorrelationContext` header’ı varsa aynen outbound isteğe eklenir.
2. Yoksa `ICorrelationContextAccessor` + `ICorrelationContextSerializer` ile serileştirilip Base64 header olarak eklenir.

Böylece zincirlenen çağrılarda tenant, kullanıcı ve telafi oturumu gibi alanlar korunur.

## 5. Telafi (compensation)

HTTP istemcisi outbound çağrı listesi tutmaz. Telafi oturumu sonlandırması RabbitMQ **`CompensationSessionFinalized`** olayı ile yapılır; ayrıntı: [Agents.Compensation.md](Agents.Compensation.md).

## 6. Hata ve yanıt modeli

- Başarılı HTTP + beklenen JSON → `ApiResponse` / `ApiResponse<T>` deserialize.
- Ağ/serileştirme hataları → `ApiResponse` içinde mesaj; üretim benzeri ortamda exception metni kısaltılır (`BiAppOptions` + host ortamı).
- Bilinen kodlar (kaynak): `deserialization_failed`, `unexpected_success_response`.

## 7. Gözlemlenebilirlik

- `IRabbitMQClient` varsa `ServiceCalledEvent` yayını için altyapı mevcut; ilgili `PublishServiceCalledEventAsync` çağrıları kaynakta çoğunlukla yorum satırıdır — etkinleştirme durumu kodu güncelleyin.

## 8. AI ajanları için

`HttpClientService` veya `HttpClientsOptions` davranışı değiştiğinde bu dosya ve gerekirse `AGENTS.md` güncellenmelidir. Yeni servis anahtarı / domain eşlemesi eklerken `appsettings` içindeki `HttpClientsOptions:Domains` ile Configuration’daki servis URL’lerinin tutarlı olduğundan emin olun.
