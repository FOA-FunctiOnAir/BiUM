# Agents.ServiceCatalog.md — Servis kataloğu manifesti (BiUM)

Mikroservisin kendi HTTP endpoint'lerini Configuration'daki servis kataloğuna (`SERVICE` / `SERVICE_PARAMETER`) endpoint bazında aktarması için kullanılır. Karşılaştırma, AI destekli alan eşleme ve yazma Configuration'dadır; BiUM yalnızca manifesti üretir ve iletir.

## 1. Endpoint'ler (`ServiceCatalogController`, `/api/base/ServiceCatalog/*`)

| Action | Metot | Görev |
|--------|-------|-------|
| `GetEndpoints` | GET | Sayfalı (`PaginatedApiResponse<ServiceCatalogEndpointDto>`) katalog adayı endpoint listesi. Sorgu (`ServiceCatalogEndpointsQuery`): `HttpMethod` (tam eşleşme), `Url` (içerir), `Q` (url ve manifestteki girdi/çıktı property adları, alt property'ler dahil), `SortBy` (`url` varsayılan, `controllerName`, `actionName`, `name`, `httpMethod`), `SortDirection`, `PageStart` (0), `PageSize` (20, en çok 100), `MicroserviceId` (ekran formundan; Configuration'a iletilir). Her kayıt: metot, url, controller, action, `[ServiceCatalog]` adı/açıklaması + durum: `Registered`, `ServiceId`, `ServiceHttpMethod` (katalogdaki servisin HTTP tipi; uçtaki metottan farklıysa ekranda gösterilir), `PreviewStatus` (`Running`/`Completed`), `PreviewId`, `PreviewRequestedAt` |
| `GetManifest` | GET | `httpMethod` + `url` için manifest (Configuration'a gitmeden) |
| `Preview` | POST | `{ httpMethod, url }` → manifest üretir, Configuration `PreviewInternalService` servisini `CallService(Ids.Service.PreviewInternalService.Id)` ile (`{ Manifest }`) çağırır, Configuration cevabını (`JsonElement`) aynen döner |
| `Apply` | POST | Yalnızca `{ previewId }`. Önizleme Configuration `GetServicePreview` servisinden `CallService(Ids.Service.GetServicePreview.Id, { Id })` ile okunur (bulunamazsa Configuration mesajı aynen döner), önizlemedeki metot + url ile manifest **yeniden** üretilir (uç bu ms'te yoksa `service_catalog_endpoint_not_found`) ve `ApplyInternalService` servisine `CallService(Ids.Service.ApplyInternalService.Id)` ile `{ Manifest, Decisions: { PreviewId } }` gönderilir. Kararlar yalnızca önizlemeden gelir; düzenleme yoktur |

- Configuration'da önizleme bir arka plan işidir: `Preview` cevabı `previewId` + `previewStatus` (`Running`/`Completed`) içerir; AI eşleşmesi tamamlanınca güncel önizleme Configuration `GET /api/configuration/Service/GetServicePreview?id=…` ile okunur (ayrıntı: BiApp.Configuration `Agents.PlatformCatalog.md`). BiUM tarafında değişiklik yoktur; cevap ve `decisions` olduğu gibi taşınır.
- **Durum yalnızca dönülecek sayfa için okunur:** filtre + sıralama + sayfalama bellekte yapılır, ardından sayfadaki uçlar (`{ Domain, MicroserviceId?, Endpoints: [{ HttpMethod, Url }] }`) tek istekle Configuration `GetFwServiceCatalogStatuses` servisine (`CallService(Ids.Service.GetServiceCatalogStatuses.Id)`) gönderilir. Durum filtresi yoktur. Configuration'a ulaşılamazsa sayfa yine döner, durum alanları `null` kalır (uyarı loglanır).
- `Preview` / `Apply` cevabı `ApiResponse<JsonElement>`; Configuration hata döndüğünde (veya BiUM erken döndüğünde) `Value` JSON `null` olarak yazılır, mesajlar kaybolmaz.
- Ekran akışı: `GetEndpoints` → satırda `previewId` yoksa (ya da yenisi isteniyorsa) `Preview { httpMethod, url }` → `GetServicePreview(previewId)` ile sonuç (`Running` iken Apply `service_catalog_preview_not_ready`) → `Apply { previewId }`.
- `BiAppOptions.Environment == Production` iken `Preview`/`Apply` `service_catalog_disabled_in_production` döner.
- Yetki Configuration tarafındadır (correlation context ile kullanıcı/tenant gider).
- Mesaj kodları: `service_catalog_endpoint_is_required`, `service_catalog_endpoint_not_found`, `service_catalog_disabled_in_production`.

## 2. Kapsam

- API Explorer (`IApiDescriptionGroupCollectionProvider`) üzerindeki `ControllerActionDescriptor`'lar.
- Hariç: `/api/base/*`, `[ServiceCatalog(false)]` (controller veya action), `[ApiExplorerSettings(IgnoreApi = true)]`.
- `[ServiceCatalog(Name = "…", Description = "…")]` (`BiUM.Core.Common.Attributes`) önerilen ad/açıklamayı manifeste ekler.

## 3. Manifest (`ServiceCatalogManifestDto`)

- `Domain`, `HttpMethod`, `Url`, `ControllerName`, `ActionName`, `Name?`, `Description?`, `ResponseIsArray`, `ResponseIsPaginated`.
- `Parameters`: query/route/form parametreleri tek tek; `[FromBody]` tipinin property'leri açılır. `Response`: `ApiResponse<T>` / `PaginatedApiResponse<T>` içindeki `T` (liste ise eleman tipi) property'leri.
- Property: `Name` (camelCase, `JsonPropertyName` öncelikli), `Source` (`Query/Body/Route/Form/Response`), `ClrType` (`Guid, String, Boolean, Integer, Long, Decimal, Double, DateTime, DateOnly, TimeOnly, Enum, Binary, File, Object`), `EnumTypeName`, `IsArray`, `IsNullable` (NRT dahil), `Children` (Object için, en fazla 4 seviye, döngü korumalı). `[JsonIgnore]` property'ler atlanır.

## 4. Kaynak

- `BiUM.Specialized/Common/API/ServiceCatalogController.cs`
- `BiUM.Specialized/Services/ServiceCatalog/` (`IServiceCatalogService`, `ServiceCatalogService`, `ServiceCatalogTypeDescriber`, `ServiceCatalogModels`)
- Testler: `tests/BiUM.Tests/ServiceCatalog/`

Configuration çağrıları katalog servis Id'leri ile yapılır (`BiUM.Core.Constants.Ids.Service.PreviewInternalService` / `ApplyInternalService` / `GetServiceCatalogStatuses` / `GetServicePreview`); SERVICE kayıtları Configuration'da bu Id'lerle tanımlı olmalıdır (scriptler: BiApp.Configuration `scripts/insert_services_ai_assessment.sql`, `scripts/insert_services_service_catalog.sql`).
