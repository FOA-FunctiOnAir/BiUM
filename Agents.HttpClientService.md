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
- İç servis tipi **Crud** için `GetFullUrl(microserviceRootPath, url)` ile mikroservis kök yolu + `base` anahtarı kullanılır; diğer iç çağrılarda `GetFullUrl(url)` yeterlidir. **`DynamicApi`** aynı kuralı paylaşacak şekilde tasarlanmıştır; `DomainDynamicApi` modeli hazır olana kadar `HttpClientService` iç çağrılarında bu dal kapalıdır (yalnızca **Crud** kök yol kullanır).
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

1. **`ICorrelationContextAccessor.CorrelationContext` doluysa** önce bu değer serileştirilip outbound header olarak eklenir (istek içinde güncellenen `CompensationSessionId` dahil tüm alanlar downstream’e böyle gider).
2. Accessor boşsa, gelen HTTP isteğindeki `CorrelationContext` header’ı varsa aynen outbound’a kopyalanır.

Böylece zincirlenen çağrılarda tenant, kullanıcı ve telafi oturumu gibi alanlar korunur; orkestrasyon servisleri Gateway’e dönmeden doğrudan MS çağırsa bile güncel bağlam taşınır.

## 5. Telafi (compensation)

HTTP istemcisi outbound çağrı listesi tutmaz. Telafi oturumu sonlandırması RabbitMQ **`CompensationSessionFinalized`** olayı ile yapılır; ayrıntı: [Agents.Compensation.md](Agents.Compensation.md).

## 6. Hata ve yanıt modeli

- Başarılı HTTP + beklenen JSON → `ApiResponse` / `ApiResponse<T>` deserialize.
- Ağ/serileştirme hataları → `ApiResponse` içinde mesaj; üretim benzeri ortamda exception metni kısaltılır (`BiAppOptions` + host ortamı).
- Bilinen kodlar (kaynak): `deserialization_failed`, `unexpected_success_response`.

## 7. Loglama (ILogger)

- **`HttpClientService`** `ILogger<HttpClientService>` kullanır.
- Outbound çağrıda **yakalanan exception** → **`LogError`** (istisna + operasyon, URL, isteğe bağlı `ServiceId`).
- HTTP yanıtı başarısız (`!IsSuccessStatusCode`) ve/veya dönen **`ApiResponse.Success == false`** (ör. downstream `MessageSeverity.Error` mesajları) → tek bir **`LogError`** satırı; ayrıntı metni `ApiResponseLogSummary.Format` ile üretilir.
- Operasyon adları: `Get`, `Post`, `CallService`, `GetServiceInfoAsync` (Configuration servis çözümlemesi sırasında yalnızca gerçek exception’larda `LogError`).

## 8. Gözlemlenebilirlik

- Önceki `ServiceCalledEvent` / `PublishServiceCalledEventAsync` yolu kaldırıldı; servisler arası çağrı izi için **`ILogger`** ve OpenTelemetry exporter’ları kullanılır.

## 9. AI ajanları için

`HttpClientService` veya `HttpClientsOptions` davranışı değiştiğinde bu dosya ve gerekirse `AGENTS.md` güncellenmelidir. Yeni servis anahtarı / domain eşlemesi eklerken `appsettings` içindeki `HttpClientsOptions:Domains` ile Configuration’daki servis URL’lerinin tutarlı olduğundan emin olun.

