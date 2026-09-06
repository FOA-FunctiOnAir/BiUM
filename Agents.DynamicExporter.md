# Agents.DynamicExporter.md — Dynamic Export (BiUM)

BiUM içinde kullanıcıya ait **asenkron Excel dışa aktarma** işleri. Kaynak veri, katalog servis URL'lerinden sayfalı HTTP GET ile çekilir.

## 1. Varlıklar

- **`DomainDynamicExportRequest`** (`__DYNAMIC_EXPORT_REQUEST`, `TenantBaseEntity`): `ApplicationId`, `Name`, `Status`, … — `TenantId`, `CorrelationId`, `CreatedBy` insert'te interceptor ile doldurulur; serviste yalnızca domain alanları set edilir.
- **`DomainDynamicExportFile`**: `ExportRequestId` FK, `Content` (byte[]) veya `StoragePath`

Durum parametreleri: `Ids.Parameter.DynamicExportRequestStatus` (Pending, Processing, Ready, Failed, Expired). Parametre ve değer GUID'leri platform kataloğunda tanımlanır; repo seed kullanılmaz — tanım sonrası `Ids.Parameter` ile hizalanır.

## 2. HTTP yüzeyi

`DynamicExporterController` (`[BiUMBaseRoute]`):

| Action | Açıklama |
|--------|----------|
| `SaveExportRequest` | `[FromBody] SaveExportRequestCommand` — yeni iş (Pending); **`CorrelationContext.User` zorunlu** |
| `GetExportRequest` | `[FromQuery] GetExportRequestQuery` (`BaseQueryDto`, `Id`) |
| `GetExportRequests` | `[FromQuery] GetExportRequestsQuery` (`BasePaginatedQueryDto`, opsiyonel `StatusId`) |
| `DeleteExportRequest` | `[FromBody] DeleteExportRequestCommand` (`BaseCommandDto`, `Id`) |
| `Download` | `[FromQuery] DownloadExportRequestQuery` — `ApiResponse<ExportDto>` (`Name`, `MimeType`, base64 `Content`); BiDynamic **Export** actionType ile uyumlu |

Görünürlük: `CreatedBy == CorrelationContext.User.Id` **ve** `TenantId == CorrelationContext.TenantId` **ve** `ApplicationId == CorrelationContext.ApplicationId` (`GetExportRequest`, `GetExportRequests`, `Download`, `DeleteExportRequest`).

**BiDynamic Export actionType:** `operationType = Export` (31) ile tanımlanan aksiyonlar GET çağrısı yapar ve yanıtta `ApiResponse<ExportDto>` bekler (`success`, `value.name`, `value.mimeType`, `value.content` base64). İstemci `downloadFile(value)` ile dosyayı indirir. Örnek servis URL: `api/base/DynamicExporter/Download`; `requestMapping` ile satır `id` → query `id` eşlenir (Coach `DownloadResourceUploadTemplate` ile aynı sözleşme).

## 3. Arka plan işleme

- `DynamicExportBackgroundService` (`IHostedService`): ~5 sn döngü; `ProcessPendingExportsAsync`, `ExpireOldExportsAsync`
- `DynamicExporterService.Worker`: kaynak URL'den `pageStart`/`pageSize` ile sayfalar (`IHttpClientsService.GetContent` — ham JSON; typed `PaginatedApiResponse` deserialize yok); `MaxExportRows`, `MaxFetchPages`, `TotalJobTimeoutMinutes`
- `DynamicExportExcelWriter`: OpenXML streaming xlsx; `ExtractRowsFromApiResponse` `value` dizisini okur

## 4. Yapılandırma (`DynamicExporterOptions`)

`appsettings` bölümü: `DynamicExporterOptions`

| Alan | Varsayılan |
|------|------------|
| `FetchPageSize` | 2000 |
| `MaxExportRows` | 1_000_000 |
| `MaxFetchPages` | 500 |
| `TotalJobTimeoutMinutes` | 60 (tüm export job; sayfa başına ayrı timeout yok — HTTP istemcisi varsayılan ~5 dk) |
| `TtlDays` | 3 |
| `SmallExportRowThreshold` | 50_000 |
| `ExportStoragePath` | null (bellek içi `Content`) |

## 5. DI

`ConfigureSpecializedServices`: `IDynamicExporterService`, `DynamicExportBackgroundService`, `DynamicExporterOptions`.

## 6. Kod konumları

- API: `DynamicExporterService.Api.cs`
- Worker: `DynamicExporterService.Worker.cs`, `DynamicExportBackgroundService`
- Excel: `DynamicExportExcelWriter.cs`
- Controller: `DynamicExporterController.cs`
