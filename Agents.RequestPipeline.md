# Agents.RequestPipeline.md — `UseInfrastructure` ve `UseSpecialized` (BiUM)

Bu belge, ASP.NET Core pipeline’da BiUM’un eklediği middleware ve filtreleri sıra ve amaçlarıyla özetler. Tam **`Program.cs`** sırası host uygulamasına aittir; burada yalnızca extension davranışı anlatılır.

## 1. `UseInfrastructure` (`BiUM.Infrastructure/ApplicationExtensions.App.cs`)

- **Swagger / SwaggerUI**: `BiAppOptions.Environment` üretim benzeri (`Production`, `Sandbox`, `Staging`, `QA`) ise kapalı; `Development` / `PreDevelopment` ise açık (host `ASPNETCORE_ENVIRONMENT` yalnızca `BiAppOptions` belirtilmemişse devreye girer). Ortak mantık: `IHostEnvironment.IsSwaggerEnabled` / `IsSwaggerUiEnabled` (`BiUM.Core/Extensions/HostEnvironmentExtensions.cs`). Gateway `SwaggerForOcelot` aynı kuralları kullanır.
- **Exception handling**
  - **gRPC** (`application/grpc`): `GrpcGlobalExceptionHandlerMiddleware` — yakalanan istisnalar **`LogError`** ile yazılır.
  - **REST**: `UseExceptionHandler` — `ApiResponse` JSON (`application/problem+json`); işlenmemiş istisna **`LogError`**.
- **Domain**: `AppDomain.UnhandledException` ve `TaskScheduler.UnobservedTaskException` loglama.
- **Middleware zinciri**: `CorrelationContextExtractorMiddleware` → `CorrelationContextActivityMiddleware` → `ServiceCallMetricsMiddleware`
  - `ServiceCallMetricsMiddleware` her HTTP isteği için `ServiceCalledEvent` yayınlar (Observability → `SERVICE_CALL_METRIC`). `/health*`, `/swagger*`, `/version`, `/index.html`, `/favicon.ico` atlanır. Controller veya action **`[ServiceCallMetric(false)]`** (`BiUM.Core.Observability`) ile işaretlenirse o endpoint için event yayınlanmaz (Audit’teki `[Auditable(false)]` gibi; action seviyesindeki attribute controller’ınkini geçersiz kılar). Kontrol istek tamamlandıktan sonra seçilen endpoint’in metadata’sından yapılır. Örnek: Observability `EventIntegrationMetricController` — Scheduler/platform action metrik çağrılarının `SERVICE_CALL_METRIC`’e gürültü olarak düşmesini engeller.
- **`UseRouting`**
- **Health**: `/health`, `/health/live`, `/health/ready` (HealthChecks UI writer)
- **`/version`**: `APP_VERSION` ortam değişkeni
- **MagicOnion**: `MapMagicOnionService()`

## 2. `UseSpecialized` (`BiUM.Specialized/ApplicationExtensions.App.cs`)

- **`RequestTransactionMiddleware`**: Mutasyon istekleri için scoped `IDbContext` üzerinde execution strategy + transaction (detay: middleware kaynak dosyası). Her denemede bir `AfterCommitEventScope` açar: commit sonrası kapsamdaki olayları yayınlar (`AfterCommitEventDispatcher`), rollback/hata durumunda atar. `TransactionalUnitOfWorkRunner` aynı davranışı HTTP dışı yollarda uygular. Bkz. [Agents.MessageBroker.md](Agents.MessageBroker.md) §4.1.1.
- **Geliştirme**: `UseMigrationsEndPoint()`
- **`UseStaticFiles`**

## 3. Transaction’dan muaf yollar (`RequestTransactionMiddlewarePolicies`)

- HTTP: **GET, HEAD, OPTIONS**
- **gRPC** (`Content-Type` `application/grpc`)
- Önek/yol: **`/health...`**, **`/swagger...`**, **`/version`**

`IDbContext` yoksa veya InMemory sağlayıcı ise transaction açılmaz.

## 4. MVC filtreleri (`AddSpecializedServices`)

`BiUM.Specialized/ApplicationExtensions.Services.cs` içinde:

- **`ApiResponseTransactionRollbackFilter`**: `ApiResponse.Success == false` → `HttpContext.Items[ApiResponseTransactionRollbackFilter.RollbackRequestedKey] = true` → `RequestTransactionMiddleware`, `next()` sonrası bu bayrağı görürse exception atmadan (normal response akışını bozmadan) DB transaction'ı rollback eder.
- **`ApiResponseLoggingFilter`**: `ObjectResult` içindeki `ApiResponse` / `ApiResponse<T>` için her `ResponseMessage` şiddetine göre log (`Error` → **`LogError`**); istek yolu log alanına eklenir.
- **`CompensatableApiActionFilter`**: Telafi oturumu; bkz. [Agents.Compensation.md](Agents.Compensation.md).

## 5. Diğer ilgili bileşenler

- **`EntitySaveChangesInterceptor`**: HTTP GET sırasında izlenen değişiklik varsa `SaveChanges` reddi (`BiUM.Specialized` — AGENTS.md).

## 6. AI ajanları için

Yeni middleware, global filtre veya exception türü eklendiğinde bu dosya ve `AGENTS.md` § Specialized güncellenmelidir.
