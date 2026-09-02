# Agents.DynamicApi.md — Domain Dynamic API (BiUM)

Bu belge **DomainDynamicApi** (tanım/metadata) ile **runtime DynamicApi** (Roslyn ile derlenen handler) ayrımını tanımlar. CRUD örüntüsü [Agents.Crud.md](Agents.Crud.md) ile paraleldir.

## 1. İki düzlem

| Düzlem | Amaç | Ana API yüzeyi |
|--------|------|----------------|
| **Tanım** | `DomainDynamicApi` oluşturma, parametreler, çeviri, yayın (`PublishDomainDynamicApi`) | `DomainDynamicApiController` → `SaveDomainDynamicApiAsync`, `DeleteDomainDynamicApiAsync`, `GetDomainDynamicApi*`, `PublishDomainDynamicApiAsync` |
| **Çalışma zamanı** | Yayınlanmış `code` ile HTTP handler çalıştırma | `DynamicApiController` → `Get`, `Post`, `Put`, `Patch`, `Delete` (`IDynamicApiService.ExecuteAsync`) |

Route: `[BiUMBaseRoute]` → `/api/base/[controller]/[action]`. Runtime action adları **Async soneksiz** (`/api/base/DynamicApi/Get/{code}`).

## 2. Varlıklar (`BiUM.Infrastructure/Common/Models`)

- **`DomainDynamicApi`** (`__DYNAMIC_API`): `ApplicationId`, `MicroserviceId`, `Code`, `HttpType`, `ExecutionType`, `RuntimePlatformType`, `SourceCode`, `Compensatible`, `CompileStatusType`, `CompileError`
- **`DomainDynamicApiParameter`**: yön, property, `FieldId`
- **`DomainDynamicApiVersion`**: yayın anı snapshot; `CompiledAssembly`, `AssemblyHash`, `EntryPointTypeName`
- **`DomainDynamicApiVersionParameter`**: versiyon parametreleri
- **`DomainDynamicApiTable`** (`__DYNAMIC_API_TABLE`): handler kaynak kodundaki `ctx.Entity(schema, table)` referanslarının kaydı; CRUD FK yok, `(DynamicApiId, Schema, TableName)` benzersiz; `(Schema, TableName)` indeksli

`BaseDbContext` / `IDbContext`: tüm `DomainDynamicApi*` DbSet'leri etkin; `(TenantId, Code)` unique index.

**Migration:** Her mikroservis kendi EF migration'ını üretmelidir (`__DYNAMIC_API_TABLE` tablosu).

## 3. Tanım düzlemi — kiracı kuralları

CRUD ile aynı System / tenant filtresi (`Ids.Customer.System.Id`). Hata kodları: `dynamic_api_definition_access_denied`, `dynamic_api_definition_not_found`, `dynamic_api_compile_failed`.

**Publish:** `DynamicApiCompiler` (Roslyn) kullanıcı `SourceCode` gövdesini `IDynamicApiHandler.ExecuteAsync` içine sarar. Kaynakta `ctx.Entity("schema","TABLE")` varsa: referanslar parse edilir → fiziksel tablo kolonları introspect edilir → EF entity + izole `DynamicTableDbContext` kodu üretilir → `ctx.Entity(...)` çağrıları `__dynamicTables.Set<T>()` ile değiştirilir → derlenir → `DomainDynamicApiVersion` binary assembly → `SaveDynamicApiServicesAsync` (`Ids.Service.SaveDynamicApiServices`) → BiApp.Configuration katalog.

**Save:** `SaveDomainDynamicApiAsync` kaynak kodu parse eder, tablo referanslarını doğrular ve `DomainDynamicApiTable` satırlarını senkronlar (publish zorunluluğu yok).

**Silme:** `DeleteDomainDynamicApiAsync` ilişkili `DomainDynamicApiTable` satırlarını kaldırır.

Tanım yardımcı uçları (`DomainDynamicApiController`):

- `GetDynamicApiSelectableTables` — query: `ApplicationId`, `MicroserviceId` (yayınlanmış CRUD tabloları + katalog şemasındaki tablolar)
- `GetDomainDynamicApisByTable` — query: `Schema`, `TableName` (ters arama)

Bu uçlar BiUM `DomainDynamicApiController` üzerindedir; **BiApp.Configuration servis kataloğu kayıtları repoda otomatik eklenmez** — Gateway üzerinden expose edilecekse katalog GUID'leri operasyon tarafından tanımlanmalıdır.

## 4. Runtime

- `DynamicApiRuntimeCache` (`IMemoryCache`): `code` + versiyon anahtarı; collectible `AssemblyLoadContext`. `ConfigureSpecializedServices` içinde `AddMemoryCache()` bu cache için zorunludur.
- `IDynamicApiExecutionContext`: `Db` (`IDbContext`), `Parameters`, `PageStart`, `PageSize`, `Correlation`, `ConnectionString`, `DatabaseType`
- Handler dönüş tipi: `ApiResponse` veya `PaginatedApiResponse<T>` (ikisi de `ApiResponse` tabanı)
- HTTP method eşleşmesi: tanımdaki `HttpType` ile route method uyuşmalı

### 4.1 `ctx.Entity(schema, table)` DSL (entity olmayan tablolar)

Handler kaynak kodunda **yalnızca string literal** argümanlar desteklenir:

```csharp
var rows = await ctx.Entity("t_a1b2…_b2c3…", "ORDERS").Where(...).ToListAsync(cancellationToken);
```

| Adım | Davranış |
|------|----------|
| Save | Roslyn ile `Entity("…","…")` çıkarılır; `DomainDynamicApiTable` upsert; fiziksel tablo zorunlu değil |
| Publish | Tablo DB'de olmalı; kolon metadata introspection (PostgreSQL / SQL Server); codegen + source transform + derleme |
| Execute | `DynamicTableDbContextFactory` izole `DynamicTableDbContext` oluşturur (aynı connection string); raw SQL gerekmez |

**İzinli şemalar:** CRUD tenant şeması `t_{16hex}_{16hex}` (`CrudSchemaHelper.ResolveSchema`), katalog şeması (`public` / PostgreSQL, `dbo` / SQL Server), SQLite test/dev için `main`.

**Engellenen tablolar:** `__CRUD*`, `__DYNAMIC*`, `__COMPENSATION*`, `__EF*`, `__TRANSLATION`, `hangfire` şeması.

**CRUD tablo eşleşmesi:** `t_*` şemasındaki tablo için `DomainCrud` kaydı bulunmalı; `ApplicationId`, `TenantId`, `MicroserviceId` Dynamic API tanımı ile uyumlu olmalı.

**Katalog (`public`/`dbo`) erişimi:** yalnızca System tenant veya `CorrelationContext.TenantId == null` bağlamında.

**Hata kodları (örnek):** `dynamic_api_entity_literal_required`, `dynamic_api_schema_not_allowed`, `dynamic_api_table_blocked`, `dynamic_api_crud_table_not_found`, `dynamic_api_crud_table_tenant_mismatch`, `dynamic_api_dbo_table_access_denied`, `dynamic_api_table_not_exists_in_db`.

### 4.2 `ctx.Db` (mevcut DbSet'ler)

Handler `ctx.Db` üzerinden **o mikroservisin** `IDbContext` bağlantısını kullanır; tanımlı DbSet'ler ve servis domain entity'leri için LINQ / EF uygundur. Başka mikroservisin veritabanına doğrudan erişim yok — HTTP/gRPC ile ilgili servise gidilmeli.

## 5. Telafi (compensation)

`DomainDynamicApi.Compensatible == true` ise `DynamicApiController` üzerindeki `Post`/`Put`/`Patch`/`Delete` mutasyonları, gelen istekte boş oturum varken `CompensatableApiActionFilter` tarafından yerel `CompensationSessionId` ile sarmalanır (CRUD ile aynı örüntü).

## 6. HTTP istemci URL çözümlemesi

`HttpClientService` iç çağrılarında `ServiceType` **Crud** veya **DynamicApi** ise `MicroserviceRootPath` + servis URL birleştirilir ([Agents.HttpClientService.md](Agents.HttpClientService.md)).

## 7. Kod konumları

- Tanım: `BiUM.Specialized/Services/DynamicApi/DynamicApiService.Definition.cs`, `DynamicApiService.Publish.cs`, `DynamicApiService.Tables.cs`
- Entity DSL: `DynamicApiEntityParser.cs`, `DynamicApiTableReferenceValidator.cs`, `DynamicApiEntityCodegen.cs`, `DynamicApiSourceTransformer.cs`, `DynamicApiTableIntrospectorFactory.cs` (+ PG/MSSQL/SQLite introspector)
- Runtime: `DynamicApiService.Runtime.cs`, `DynamicApiCompiler.cs`, `DynamicApiRuntimeCache.cs`, `DynamicApiDbContextOptions.cs`
- HTTP: `DomainDynamicApiController.cs`, `DynamicApiController.cs`
- Mikroservis kodu: `MicroserviceCodeHelper.FromRootPath` (`/api/education/coach` → `education-coach`)

## 8. Parametre kimlikleri

Platform kataloğunda tanımlanır (repo seed değil); ardından `BiUM.Core.Constants.Ids.Parameter` içindeki `Guid.Parse` değerleri ile hizalanır:

- `DynamicExportRequestStatus`: Pending / Processing / Ready / Failed / Expired
- `DynamicApiCompileStatus`: Draft / Success / Failed
- `DynamicApiExecutionType`: CSharpEf
- `Ids.Parameter.ServiceType.Values.DynamicApi` (mevcut sabit)
- `Ids.Service.SaveDynamicApiServices` (Configuration callback servisi)
