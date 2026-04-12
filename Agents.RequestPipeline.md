# Agents.RequestPipeline.md — `UseInfrastructure` ve `UseSpecialized` (BiUM)

Bu belge, ASP.NET Core pipeline’da BiUM’un eklediği middleware ve filtreleri sıra ve amaçlarıyla özetler. Tam **`Program.cs`** sırası host uygulamasına aittir; burada yalnızca extension davranışı anlatılır.

## 1. `UseInfrastructure` (`BiUM.Infrastructure/ApplicationExtensions.App.cs`)

- **Swagger / SwaggerUI**: Geliştirme veya `BiAppOptions.Environment` üretim benzeri olmayan ortamlarda.
- **Exception handling**
  - **gRPC** (`application/grpc`): `GrpcGlobalExceptionHandlerMiddleware` — yakalanan istisnalar **`LogError`** ile yazılır.
  - **REST**: `UseExceptionHandler` — `ApiResponse` JSON (`application/problem+json`); işlenmemiş istisna **`LogError`**; **`ApiResponseRollbackException`** için `application/json`, taşınan status ve `ApiResponse` gövdesi; yanıt yazılmadan önce taşınan mesajlar **`LogError`** ile özetlenir (`ApiResponseLogSummary`).
- **Domain**: `AppDomain.UnhandledException` ve `TaskScheduler.UnobservedTaskException` loglama.
- **Middleware zinciri**: `CorrelationContextExtractorMiddleware` → `CorrelationContextActivityMiddleware` → `ServiceCallMetricsMiddleware`
- **`UseRouting`**
- **Health**: `/health`, `/health/live`, `/health/ready` (HealthChecks UI writer)
- **`/version`**: `APP_VERSION` ortam değişkeni
- **MagicOnion**: `MapMagicOnionService()`

## 2. `UseSpecialized` (`BiUM.Specialized/ApplicationExtensions.App.cs`)

- **`RequestTransactionMiddleware`**: Mutasyon istekleri için scoped `IDbContext` üzerinde execution strategy + transaction (detay: middleware kaynak dosyası).
- **Geliştirme**: `UseMigrationsEndPoint()`
- **`UseStaticFiles`**

## 3. Transaction’dan muaf yollar (`RequestTransactionMiddlewarePolicies`)

- HTTP: **GET, HEAD, OPTIONS**
- **gRPC** (`Content-Type` `application/grpc`)
- Önek/yol: **`/health...`**, **`/swagger...`**, **`/version`**

`IDbContext` yoksa veya InMemory sağlayıcı ise transaction açılmaz.

## 4. MVC filtreleri (`AddSpecializedServices`)

`BiUM.Specialized/ApplicationExtensions.Services.cs` içinde:

- **`ApiResponseTransactionRollbackFilter`**: `ApiResponse.Success == false` → `ApiResponseRollbackException` → REST handler + DB transaction rollback.
- **`ApiResponseLoggingFilter`**: `ObjectResult` içindeki `ApiResponse` / `ApiResponse<T>` için her `ResponseMessage` şiddetine göre log (`Error` → **`LogError`**); istek yolu log alanına eklenir.
- **`CompensatableApiActionFilter`**: Telafi oturumu; bkz. [Agents.Compensation.md](Agents.Compensation.md).

## 5. Diğer ilgili bileşenler

- **`EntitySaveChangesInterceptor`**: HTTP GET sırasında izlenen değişiklik varsa `SaveChanges` reddi (`BiUM.Specialized` — AGENTS.md).

## 6. AI ajanları için

Yeni middleware, global filtre veya exception türü eklendiğinde bu dosya ve `AGENTS.md` § Specialized güncellenmelidir.
