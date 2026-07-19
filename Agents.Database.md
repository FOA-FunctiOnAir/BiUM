# Agents.Database.md — `AddDatabase` ve DbContext kaydı (BiUM)

Bu belge, **`BiUM.Specialized.Database.Extensions.AddDatabase`** ile tipik bir mikroserviste EF Core’un nasıl kaydedildiğini özetler.

## 1. API

```csharp
services.AddDatabase<TDbContext, TDbContextInitialiser>(configuration);
```

- **`TDbContext`**: `DbContext` + **`IDbContext`** (`DomainDynamicApi*` şu an `IDbContext` / `BaseDbContext` modelinde yok; ayrıntı: [Agents.Crud.md](Agents.Crud.md))
- **`TDbContextInitialiser`**: `IDbContextInitialiser` uygulayan seed/migration yardımcısı

Kaynak: `BiUM.Specialized/Database/Extensions.cs` (partial sınıfın bu dosyadaki bölümü).

## 2. `DatabaseType` dallanması

| `configuration["DatabaseType"]` | Davranış |
|-----------------------------------|----------|
| `InMemory` | `UseInMemoryDatabase("InMemoryDb")` — retry yok |
| `MSSQL` | `UseSqlServer` + optional migrations assembly (when adopted) + **`EnableRetryOnFailure(5, 10s)`** + **`UseQuerySplittingBehavior(SplitQuery)`** |
| `PostgreSQL` (veya diğer) | `NpgsqlConnectionStringBuilder` ile pooling (`MaxPoolSize` 100, `KeepAlive` 30) + optional migrations assembly (when adopted) + **`EnableRetryOnFailure(5, 10s)`** + **`UseQuerySplittingBehavior(SplitQuery)`** |

**Çoklu koleksiyon `Include`:** İlişkisel sağlayıcılarda varsayılan **bölünmüş sorgu** (`SplitQuery`), EF’nin “multiple collection include” uyarısını giderir ve tek sorguda kartesyen çoğalmayı azaltır; ek round-trip maliyeti vardır. Tek sorgu şart olan yerde `IQueryable` üzerinde **`.AsSingleQuery()`** ile geçersizlenebilir.

## 3. DI ve sağlık

- **`AddScoped<IDbContext>(sp => sp.GetRequiredService<TDbContext>())`**: Uygulama kodu hem somut context hem arayüz ile çalışabilir.
- **`AddScoped<IDbContextInitialiser, TDbContextInitialiser>`**
- **`AddDatabaseDeveloperPageExceptionFilter`**
- **`AddHealthChecks().AddDbContextCheck<TDbContext>()`**

## 4. İstek transaction ile ilişki

- MSSQL ve PostgreSQL yollarında retry açık olduğu için **`RequestTransactionMiddleware`** içinde **`IExecutionStrategy`** kullanımı zorunludur; aksi halde retry + kullanıcı transaction’ı çakışır.
- InMemory sağlayıcıda middleware transaction açmaz (`RequestTransactionMiddlewarePolicies.IsInMemoryDatabaseProvider`).

## 5. Sorgu yardımcıları (aynı dosyada)

- **`OrderQuery`**, **`OrderPaginatedQuery`**, **`OrderByProperty`**: `IBaseQuery` sıralama ve sayfalama parametreleri ile `IQueryable` düzenleme.

## 6. EF Core migrations (BiApp microservices)

Migration projeleri mikroservis repo kökündeki **`add-migration.ps1`** ile lazy oluşturulur. Assembly adı `BiAppOptions:Domain` ve `DatabaseType` (`PostgreSQL` / `MSSQL`) ile türetilir:

| Context | PostgreSQL assembly | MSSQL assembly |
|---------|---------------------|----------------|
| Main (`AddDatabase`) | `BiApp.{Domain}.Migrations.Postgres` | `BiApp.{Domain}.Migrations.Mssql` |
| Bolt (`AddBolt`) | `BiApp.{Domain}.Migrations.Postgres.Bolt` | `BiApp.{Domain}.Migrations.Mssql.Bolt` |

**Startup:** `DbContextInitialiser` / `BoltDbContextInitialiser` migration assembly yüklü ve en az bir `Migration` sınıfı varsa yalnızca **`MigrateAsync`** çalıştırır; aksi halde **`EnsureCreatedAsync`** (mevcut ortamlar kırılmadan kademeli geçiş).

**Üretim:** `.\add-migration.ps1 MigrationName` — Postgres ve MSSQL için aynı isimle main migration; `AddBolt` varsa Bolt context için de aynı isimle dört proje güncellenir.

Kaynak: `BiUM.Specialized/Database/EfMigrationsAssemblyResolver.cs`, `EfDatabaseInitialiser.cs`, repo kökü `add-migration.ps1`.

## 7. AI ajanları için

Bağlantı seçenekleri, retry sayıları veya `IDbContext` yaşam döngüsü değişirse bu dosya ve [Agents.RequestPipeline.md](Agents.RequestPipeline.md) güncellenmelidir.
