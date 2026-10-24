# Agents.ServiceCatalog.md — Servis kataloğu manifesti (BiUM)

Mikroservisin kendi HTTP endpoint'lerini Configuration'daki servis kataloğuna (`SERVICE` / `SERVICE_PARAMETER`) endpoint bazında aktarması için kullanılır. Karşılaştırma, AI destekli alan eşleme ve yazma Configuration'dadır; BiUM yalnızca manifesti üretir ve iletir.

## 1. Endpoint'ler (`ServiceCatalogController`, `/api/base/ServiceCatalog/*`)

| Action | Metot | Görev |
|--------|-------|-------|
| `GetEndpoints` | GET | Katalog adayı endpoint listesi (metot, url, controller, action, `[ServiceCatalog]` adı/açıklaması) |
| `GetManifest` | GET | `httpMethod` + `url` için manifest (Configuration'a gitmeden) |
| `Preview` | POST | `{ httpMethod, url }` → manifest üretir, Configuration `PreviewInternalService` servisini `CallService(Ids.Service.PreviewInternalService.Id)` ile (`{ Manifest }`) çağırır, Configuration cevabını (`JsonElement`) aynen döner |
| `Apply` | POST | `{ httpMethod, url, decisions }` → manifesti **yeniden** üretir, Configuration `ApplyInternalService` servisini `CallService(Ids.Service.ApplyInternalService.Id)` ile (`{ Manifest, Decisions }`) çağırır; `decisions` biçimi Configuration'a aittir (önizlemeden dönen `previewId` `decisions.previewId` olarak gönderilir) |

- Configuration'da önizleme bir arka plan işidir: `Preview` cevabı `previewId` + `previewStatus` (`Running`/`Completed`) içerir; AI eşleşmesi tamamlanınca güncel önizleme Configuration `GET /api/configuration/Service/GetServicePreview?id=…` ile okunur (ayrıntı: BiApp.Configuration `Agents.PlatformCatalog.md`). BiUM tarafında değişiklik yoktur; cevap ve `decisions` olduğu gibi taşınır.
- Configuration URL ile çağrılır (`IHttpClientsService.Post`, `HttpClientsOptions.Domains["configuration"]`); servis kataloğunda kayıt gerekmez.
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

Configuration çağrıları katalog servis Id'leri ile yapılır (`BiUM.Core.Constants.Ids.Service.PreviewInternalService` / `ApplyInternalService`); SERVICE kayıtları Configuration'da bu Id'lerle tanımlı olmalıdır (dev script: BiApp.Configuration `scripts/insert_services_ai_assessment.sql`).
