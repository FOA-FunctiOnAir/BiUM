# Agents.Database.md — `AddDatabase` ve DbContext kaydı (BiUM)

Bu belge, **`BiUM.Specialized.Database.Extensions.AddDatabase`** ile tipik bir mikroserviste EF Core’un nasıl kaydedildiğini özetler.

## 1. API

```csharp
services.AddDatabase<TDbContext, TDbContextInitialiser>(configuration);
```

- **`TDbContext`**: `DbContext` + **`IDbContext`** (`DomainDynamicApi*` şu an `IDbContext` / `BaseDbContext` modelinde yok; ayrıntı: [Agents.Crud.md](Agents.Crud.md))
- **`TDbContextInitialiser`**: `IDbContextInitialiser` uygulayan seed/migration yardımcısı

Kaynak: `BiUM.Specialized/Database/Extensions.cs` (partial sınıfın bu dosyadaki bölümü).

## 2. Konfigürasyon (`Databases`) ve sağlayıcı dallanması

Tüm servisler tek formatı kullanır (eski kök `DatabaseType` ve `ConnectionStrings` anahtarları kaldırıldı; geriye uyumluluk yoktur):

```json
"Databases": {
  "Default":  { "Type": "PostgreSQL", "ConnectionString": "Host=...; Database=my_service; ..." },
  "Readings": { "Type": "PostgreSQL", "ConnectionString": "Host=...; Database=my_service_readings; ..." }
}
```

- Sınıf: `BiUM.Core/Common/Configs/DatabaseOptions.cs` (`Name = "Databases"`, `DefaultKey = "Default"`, `Type`, `ConnectionString`) ve `DatabaseTypes` (`PostgreSQL`, `MSSQL`, `InMemory`).
- Okuma: `BiUM.Core.Extensions.DatabaseConfigurationExtensions` — `configuration.GetDatabaseOptions(name = "Default")`, `GetDatabaseType(name)`, `GetRequiredDatabaseConnectionString(name)`. BiUM içinde (`AddDatabase`, `EfMigrationsAssemblyResolver`, Bolt ana DB adı, `CrudService`, `DynamicApiService`, `CompensationService`) ve mikroservislerde (ör. Scheduler Hangfire) bağlantı/tip yalnızca bu yardımcılarla okunur.
- `AddDatabase` her zaman **`Databases:Default`** kullanır. İsimli ek bağlantılar (`Databases:<Name>`) EF context’i değil, **native database** olarak kaydedilir: [Agents.NativeDatabase.md](Agents.NativeDatabase.md).
- `BoltOptions.ConnectionString` ayrı kalır; Bolt DB adı `{0}` yerine `Databases:Default:ConnectionString` içindeki `Database` değeriyle doldurulur.
- Ortam değerleri `BiApp.Infrastructure/charts/services/<env>/<service>/values.yaml` içindeki gömülü appsettings JSON’unda aynı formattadır; yeni servis şablonu BiUM.Generator `AppSettings.mustache`.

| `Databases:Default:Type` | Davranış |
|---------------------------|----------|
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

## 5. Sorgu yardımcıları ve çeviri dil çözümlemesi

- **`OrderQuery`**, **`OrderPaginatedQuery`**, **`OrderByProperty`**: `IBaseQuery` sıralama ve sayfalama parametreleri ile `IQueryable` düzenleme. **`ToPaginatedListAsync<TSource,TDestination>`** / **`WhereToPaginatedListAsync`**: `OrderPaginatedQuery` **kaynak entity** üzerinde (SQL `ORDER BY` / `OFFSET` / `FETCH`, varsayılan `Created`); sonra `ProjectTo`. DTO üzerinde sıralama yok (`Created` DTO’da olmayabilir; in-memory sıralama yok). `SortBy` entity özellik adı olmalıdır.
- **`PaginationQuery.ToPageBaseQuery`**: sayfalama-only `IBaseQuery` (repository’lerde `ToPaginatedListAsync(..., PaginationQuery.ToPageBaseQuery(pageStart, pageSize), mapper, cancellationToken)`).
- **`CorrelationContextLanguage`** (`BiUM.Specialized/Database/CorrelationContextLanguage.cs`): `UseSpecialized()` içinde `ICorrelationContextAccessor` ile yapılandırılır. `ProjectTo` öncesi dil: açık `languageId` (boş olmayan `Guid`) → `CorrelationContext.LanguageId` → `CorrelationContext.DefaultLanguageId`.
- **EF extension overload’ları** (`Extensions.*.cs`): çeviri içeren `ProjectTo` yolunda varsayılan overload **`languageId` almaz** — dil bağlamdan gelir. Farklı dil gereken nadir API’ler için aynı metodların **`Guid languageId`** parametreli overload’ı vardır. Mikroservis repository’leri normalde yalnızca bağlam overload’ını kullanır; **`CorrelationContext.LanguageId`’yi extension argümanı olarak geçirmeyin**. Sorgu filtrelerinde (`Include`, `Where`, cache key) dil kullanımı ayrı kalır.
- **AutoMapper çeviri ifadeleri** (`BiUM.Specialized.Mapping.TranslationMapping`): DTO `Mapping` profillerinde `GetColumnTranslationExpr` / `GetColumnTranslationsExpr` için `using static BiUM.Specialized.Mapping.TranslationMapping`; `NameTr` kaydetme yollarında `ToTranslationString()` için `using BiUM.Specialized.Mapping`. `System.Linq.Extensions` ile paylaşılan tip adı yok (`BiUM.Core` enumerable helper’ları ayrı kalır).
- **`GetColumnTranslationExpr` maliyeti ve zorunlu index:** Her `ForMember(..., GetColumnTranslationExpr(...))` çağrısı, `ProjectTo` ile üretilen SQL'de **ayrı bir korelasyonlu alt-sorgu** haline gelir (`WHERE RecordId=... AND Column=... [AND LanguageId=...]`, `FirstOrDefault`). Bir DTO aynı entity için birden çok çevrilebilir kolonu (`Name`, `Name2`, `Name3`, ...) bu şekilde map'liyorsa, N kolon = N ayrı alt-sorgu — satır başına maliyet doğrusal çarpılır. `TranslationBaseEntity`'den türeyen her `*_TRANSLATION` tablosunda varsayılan scaffold sadece `RECORD_ID` üzerinde index oluşturur; bu, 2+ kolonlu DTO'larda **yetersizdir**. Böyle bir çeviri tablosunu sık çağrılan/sayfalanan bir endpoint'te kullanan her mikroservis, o tabloya şu composite index'i eklemelidir: `(RECORD_ID, COLUMN, LANGUAGE_ID)`. 3+ çevrilebilir kolonu olan DTO'lar için (`GetColumnTranslationExpr`'i 3+ kez çağıran mapping profilleri), tercih edilen desen N ayrı `ForMember`/alt-sorgu yerine repository metodunda `.Include(x => x.Translations.Where(t => allowedColumns.Contains(t.Column) && (languageId == default || t.LanguageId == languageId)))` ile TEK join üzerinden çekip DTO'yu bellekte (ucuz, satır başına birkaç kolon) elle doldurmaktır — bu N korelasyonlu alt-sorguyu 1 join'e indirir. Çalışan örnek: `BiApp.Parameters/Parameters.Infrastructure/Repositories/ParameterRepository.cs` → `GetAllStaticParameterValues`/`MapStaticParameterValueToDto` (5 kolonlu `GetFwParameterValuesDto` için uygulanmıştır; `GetColumnTranslationExpr`'in null-fallback davranışı — çeviri satırı yoksa entity'nin ham kolonuna DÜŞMEZ, `null` döner — korunmalıdır).

## 6. EF Core migrations (BiApp microservices)

Migration projeleri mikroservis repo kökündeki **`add-migration.ps1`** ile lazy oluşturulur (script sağlayıcıyı `Databases__Default__Type` ortam değişkeniyle değiştirir). Assembly adı `BiAppOptions:Domain` ve `Databases:Default:Type` (`PostgreSQL` / `MSSQL`) ile türetilir:

| Context | PostgreSQL assembly | MSSQL assembly |
|---------|---------------------|----------------|
| Main (`AddDatabase`) | `BiApp.{Domain}.Migrations.Postgres` | `BiApp.{Domain}.Migrations.Mssql` |
| Bolt (`AddBolt`) | `BiApp.{Domain}.Migrations.Postgres.Bolt` | `BiApp.{Domain}.Migrations.Mssql.Bolt` |

**Startup:** `DbContextInitialiser` / `BoltDbContextInitialiser` migration assembly yüklü ve en az bir `Migration` sınıfı varsa yalnızca **`MigrateAsync`** çalıştırır; aksi halde **`EnsureCreatedAsync`** (mevcut ortamlar kırılmadan kademeli geçiş).

**Migrations assembly kaydı:** `GetActiveMigrationsAssemblyName` migration projesi derlenip yüklenebiliyorsa (henüz `Migration` sınıfı olmasa bile) `MigrationsAssembly(...)` ayarlar; böylece ilk migration (`Initial`) da `dotnet ef` ile hedef projeye üretilebilir. Migrate / EnsureCreated kararı ayrı olarak `ShouldUseMigrations` (en az bir `Migration` sınıfı) ile verilir.

**Üretim:** `.\add-migration.ps1 MigrationName` — Postgres ve MSSQL için aynı isimle main migration; `AddBolt` varsa Bolt context için de aynı isimle dört proje güncellenir.

Kaynak: `BiUM.Specialized/Database/EfMigrationsAssemblyResolver.cs`, `EfDatabaseInitialiser.cs`, repo kökü `add-migration.ps1`.

## 7. AI ajanları için

Bağlantı seçenekleri, retry sayıları veya `IDbContext` yaşam döngüsü değişirse bu dosya ve [Agents.RequestPipeline.md](Agents.RequestPipeline.md) güncellenmelidir.
