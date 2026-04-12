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
| `MSSQL` | `UseSqlServer` + migrations assembly + **`EnableRetryOnFailure(5, 10s)`** |
| `PostgreSQL` (veya diğer) | `NpgsqlConnectionStringBuilder` ile pooling (`MaxPoolSize` 100, `KeepAlive` 30) + **`EnableRetryOnFailure(5, 10s)`** |

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

## 6. AI ajanları için

Bağlantı seçenekleri, retry sayıları veya `IDbContext` yaşam döngüsü değişirse bu dosya ve [Agents.RequestPipeline.md](Agents.RequestPipeline.md) güncellenmelidir.
