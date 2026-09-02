# Agents.DynamicExporter.md — Dynamic Export (BiUM)

BiUM içinde kullanıcıya ait **asenkron Excel dışa aktarma** işleri. Kaynak veri, katalog servis URL'lerinden sayfalı HTTP GET ile çekilir.

## 1. Varlıklar

- **`DomainDynamicExportRequest`** (`__DYNAMIC_EXPORT_REQUEST`, `TenantBaseEntity`): `ApplicationId`, `Name`, `Status`, `SourceUrl`, `SourceMicroserviceId`, `SourceParameters` (JSON), `Format`, `FileName`, `MimeType`, `RowCount`, `SourceTotalCount`, `Truncated`, `ErrorMessage`, `ExpiresAt`
- **`DomainDynamicExportFile`**: `ExportRequestId` FK, `Content` (byte[]) veya `StoragePath`

Durum parametreleri: `Ids.Parameter.DynamicExportRequestStatus` (Pending, Processing, Ready, Failed, Expired). Parametre ve değer GUID'leri platform kataloğunda tanımlanır; repo seed kullanılmaz — tanım sonrası `Ids.Parameter` ile hizalanır.

## 2. HTTP yüzeyi

`DynamicExporterController` (`[BiUMBaseRoute]`):

| Action | Açıklama |
|--------|----------|
| `SaveExportRequest` | Yeni iş (Pending) |
| `GetExportRequest` | Tek kayıt |
| `GetExportRequests` | Kullanıcının listesi (sayfalı) |
| `DeleteExportRequest` | Sil |
| `Download` | Hazır dosya stream |

Görünürlük: `CreatedBy == CorrelationContext.User.Id`.

## 3. Arka plan işleme

- `DynamicExportBackgroundService` (`IHostedService`): ~5 sn döngü; `ProcessPendingExportsAsync`, `ExpireOldExportsAsync`
- `DynamicExporterService.Worker`: kaynak URL'den `pageStart`/`pageSize` ile sayfalar; `MaxExportRows`, `MaxFetchPages`, job/page timeout
- `DynamicExportExcelWriter`: OpenXML streaming xlsx; `ExtractRowsFromApiResponse` `value` dizisini okur

## 4. Yapılandırma (`DynamicExporterOptions`)

`appsettings` bölümü: `DynamicExporterOptions`

| Alan | Varsayılan |
|------|------------|
| `FetchPageSize` | 2000 |
| `MaxExportRows` | 1_000_000 |
| `MaxFetchPages` | 500 |
| `PerPageTimeoutSeconds` | 60 |
| `TotalJobTimeoutMinutes` | 60 |
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
