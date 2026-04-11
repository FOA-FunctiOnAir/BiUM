# Agents.Compensation.md — Telafi (compensation) oturumu (BiUM)

Bu belge, dağıtık senaryolarda **telafi oturumu** (`CompensationSessionId`), anlık görüntüler (`DomainCompensationSnapshot`) ve diğer mikroservislerde **commit/rollback** sonlandırmasını özetler.

## 1. İstek işlemi ve veritabanı işlemi

- **`RequestTransactionMiddleware`** (`BiUM.Specialized/Middlewares/RequestTransactionMiddleware.cs`): Scoped `IDbContext` için `IExecutionStrategy.ExecuteAsync` içinde `BeginTransactionAsync` → pipeline → başarılıysa commit, hata varsa rollback. EF `EnableRetryOnFailure` ile uyumludur.
- Salt-okuma istekleri bu sarmalayıcıdan hariç tutulur; ayrıntılar: [Agents.RequestPipeline.md](Agents.RequestPipeline.md).
- Başarısız **`ApiResponse`** (HTTP 200 + `Success == false`) durumunda **`ApiResponseTransactionRollbackFilter`** `ApiResponseRollbackException` fırlatır; REST exception handler bunu JSON olarak yazar ve istek transaction’ı geri alınır.

## 2. Olay tabanlı sonlandırma (RabbitMQ)

Ana (orkestrasyon) API, yerelde `CommitSessionAsync` / `RollbackSessionAsync` çalıştırdıktan sonra **`CompensationSessionFinalized`** olayını yayınlar. Diğer mikroservisler aynı `CompensationSessionId` ile bu olayı tüketip kendi veritabanlarında aynı işlemi uygular.

| Öğe | Konum |
|-----|--------|
| Olay | `BiUM.Core/MessageBroker/Events/CompensationSessionFinalizedEvent.cs` — `CompensationSessionId`, `Success` (true = commit, false = rollback) |
| Yayınlama | `ICompensationSessionFinalizedPublisher` / `CompensationSessionFinalizedPublisher` (`BiUM.Infrastructure`) — `IRabbitMQClient.PublishAsync` |
| Tüketim | `CompensationSessionFinalizedHandler` (`BiUM.Specialized`) — `IEventHandler<CompensationSessionFinalizedEvent>`; `CommitSessionAsync` veya `RollbackSessionAsync` |

HTTP `POST /Compensation/commit|rollback` **yoktur**. Acil durum veya yeniden deneme için aynı olay mesajını broker üzerinden yeniden yayınlamak gerekir.

## 3. `ICompensationService`

- Arayüz: `BiUM.Specialized/Services/Compensation/ICompensationService.cs` — `CommitSessionAsync`, `RollbackSessionAsync`.
- Uygulama: `CompensationService.cs` — `IDbContext` **`BaseDbContext`** olmalı (aksi halde ctor hata verir).
- **EF varlıkları**: `EntityClrTypeName` dolu anlık görüntüler; `ICompensatableEntity` için `CStatus` güncellenir.
- **Dinamik CRUD**: `ApplicationId`, `SnapshotTableName`, `OperationType`, `OldDataJson` ile PostgreSQL / MSSQL için şema `t_{app16}_{tenant16}` kuralına göre çözülür (`ResolveSchema`).
- **`DatabaseType`** yapılandırması: `CommitCrudRowAsync` / `RollbackCrudSnapshotAsync` için; varsayılan mantık kaynakta PostgreSQL.

Yerel commit/rollback yalnızca **Pending** anlık görüntü satırlarında işlem yapar; tekrarlayan olay teslimatları pratikte **idempotent** kalır.

## 4. Ana orkestrasyon API (`CompensatableApi`)

**`CompensatableApiActionFilter`** (`BiUM.Specialized/Common/API/CompensatableApiActionFilter.cs`) `AddSpecializedServices` içinde MVC filtreleri olarak **global** eklenir (`ApplicationExtensions.Services.cs`).

- **`[CompensatableApi]`** (`CompensatableApiAttribute`): Controller veya action üzerinde olmalıdır; **yalnızca** bu işaretli uçlar ana telafi API’si olarak oturum açıp sonlandırıp olayı yayınlar. İşaretsiz uçlar filtreden erken çıkar (downstream çağrılar gelen `CompensationSessionId` ile normal işler).
- İşaretli uçta, **`CrudController`** üzerinde `SaveAsync`, `SavePartialAsync`, `DeleteAsync` ve rota `code` için `ICrudService.IsCrudMutationCompensatibleByCodeAsync` true ise ve gelen istekte **`CompensationSessionId` yoksa**, yeni oturum: `CorrelationContext.WithCompensationSessionId`.
- Aksiyon sonrası: başarısız sonuç veya exception → **`RollbackSessionAsync`** + **`PublishAsync(..., success: false)`**; başarı → **`CommitSessionAsync`** + **`PublishAsync(..., success: true)`**.

## 5. Bağlam yayılımı

- **`CorrelationContext.CompensationSessionId`**: zincirde taşınması için HTTP’de `x-correlation-context` (ve RabbitMQ header’ları) kullanılır.
- Ayrıntı: [Agents.CorrelationContext.md](Agents.CorrelationContext.md).

## 6. İlgili kod konumları

- Middleware politikaları: `RequestTransactionMiddlewarePolicies.cs`
- Rollback sinyali: `BiUM.Contract/Models/Api/ApiResponseRollbackException.cs`, `ApiResponseTransactionRollbackFilter.cs`
- Olay yayını: `RabbitMQClient` içinde `CompensationSessionFinalizedEvent` fanout yolu

## 7. AI ajanları için

Telafi anlık görüntü şeması, `C_STATUS` anlamları veya olay sözleşmesi değişirse bu dosya ve [Agents.Crud.md](Agents.Crud.md) güncellenmelidir.
