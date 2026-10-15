# Agents.NativeDatabase.md — Native SQL ve ek veritabanları (BiUM)

EF Core’un uygun olmadığı işler (yüksek hacimli zaman serisi, binary COPY, partition yönetimi, partitioned tablo DDL’i) için BiUM **native database** katmanı sağlar. Mikroservisler ham `NpgsqlConnection` / `ExecuteSqlRaw` / `GetDbConnection` kullanmaz; SQL her zaman bu katmandaki **tipli statement** sınıflarıyla çalıştırılır.

Bugün yalnızca **PostgreSQL** desteklenir (başka `Type` verilirse `NotSupportedException`).

## 1. Kayıt

```json
"Databases": {
  "Default":  { "Type": "PostgreSQL", "ConnectionString": "...Database=my_service;..." },
  "Readings": { "Type": "PostgreSQL", "ConnectionString": "...Database=my_service_readings;..." }
}
```

```csharp
services.AddBiUMNativeDatabase(configuration, "Readings", typeof(SomeInfrastructureType).Assembly);

await scope.ServiceProvider.InitialiseNativeDatabases();
```

- `AddBiUMNativeDatabase` (`BiUM.Specialized/Database/Native/NativeDatabaseServiceCollectionExtensions.cs`): `Databases:<Name>` okunur; pooling’li keyed `NpgsqlDataSource`, keyed **`INativeDatabase`**, migration listesi ve `native-database-<name>` health check’i (tag `ready`) kaydedilir. Aynı isim ikinci kez eklenmez.
- Tüketim: `[FromKeyedServices("Readings")] INativeDatabase readingsDatabase`.
- `InitialiseNativeDatabases()`: her kayıt için veritabanı yoksa oluşturur (`postgres` bakım DB’sine bağlanıp `CREATE DATABASE`, paralel pod’larda `DuplicateDatabase` yutulur), ardından migration’ları uygular.
- Npgsql OpenTelemetry izleme (`AddNpgsql()`) native komutları da kapsar. 2 sn’yi aşan komutlar `Warning` loglanır.
- Native komutlar **istek transaction’ına (`RequestTransactionMiddleware`) dahil değildir**; her çağrı kendi bağlantısı/transaction’ı ile çalışır.

## 2. Migration’lar

```csharp
public sealed class M0001InitialSchema : NativeSchemaMigration
{
    public override string Database => "Readings";
    public override string Id => "0001_initial_schema";
    public override string Sql => """ CREATE TABLE ... PARTITION BY RANGE (...); """;
}
```

- `NativeSchemaMigration` (`BiUM.Core/Database/Native`) türevleri verilen assembly’lerden taranır; `Database` adı eşleşenler alınır.
- `Id` sırasıyla (ordinal) uygulanır; her migration kendi transaction’ında çalışır ve `public."__NATIVE_MIGRATION_HISTORY"` tablosuna yazılır. Eşzamanlı başlatmalar `pg_advisory_lock` ile sıraya girer.
- Uygulanmış bir migration değiştirilmez; değişiklik için yeni `Id` eklenir.

## 3. Statement’lar

| Tip | Kullanım |
|-----|----------|
| `NativeSqlCommand` | `ExecuteAsync` — `Sql` + `GetParameters()` |
| `NativeSqlQuery<T>` | `QueryAsync` — ayrıca `Map(DbDataReader)` |
| `NativeBulkMerge<TRow>` | `BulkMergeAsync` — `Schema`, `Table`, `Columns` (`NativeBulkColumn<TRow>(name, NativeColumnType, getter)`), `ConflictColumns`, `ConflictAction` (`DoNothing` / `DoUpdate`) |

- Parametreler `NativeSqlParameter(name, value, NativeColumnType?)` ile verilir; SQL’de `@name`. SQL metni sabit tutulur, değerler string birleştirme ile eklenmez.
- `BulkMergeAsync`: tek transaction’da `CREATE TEMP TABLE ... (LIKE target) ON COMMIT DROP` → binary `COPY` → `INSERT ... SELECT ... ON CONFLICT (...) DO NOTHING | DO UPDATE SET` (çakışma kolonları dışındaki kolonlar). Tekrarlanan teslimlerde (RabbitMQ redelivery) tek satırlık çakışma tüm batch’i düşürmez.
- `NativeColumnType`: Boolean, Int16, Int32, Int64, Double, Numeric, Text, Uuid, TimestampTz, Date ve dizi tipleri (Int16/Int32/Int64/Double/Uuid/Text array). `DateTime` değerleri UTC olmalıdır.

## 4. Partition yönetimi

```csharp
var set = new NativeRangePartitionSet("dbo", "READING", NativePartitionInterval.Day);
await db.EnsureRangePartitionsAsync(set, from, to, ct);
await db.DropRangePartitionsBeforeAsync(set, before, ct);
```

- Partition adı `{ParentTable}_{yyyyMMdd|yyyyMM|yyyy}`; `CREATE TABLE IF NOT EXISTS ... PARTITION OF ... FOR VALUES FROM (...) TO (...)`, `pg_advisory_xact_lock` ile korunur.
- Drop, adı parse edilebilen ve tamamen `before` öncesinde kalan partition’ları siler (retention için `DELETE` yerine kullanılır).
- PostgreSQL partitioned tabloda partition anahtarı PK’ya dahil olmalıdır; partition yoksa insert `23514` (check violation) ile düşer — çağıran taraf `EnsureRangePartitionsAsync` + tekrar deneme uygulamalıdır.

## 5. Örnek kullanım

- **sample2 (Test2)**: `Databases:Metrics`, `Test2.Infrastructure/Persistence/Native/SampleMetrics.cs` (migration, `SampleMetricMerge` DoUpdate, `SampleMetricRangeQuery`, `DeleteSampleMetricsCommand`, günlük partition seti) ve `TestNativeDatabaseController` (`EnsurePartitions`, `WriteMetrics`, `GetMetrics`, `DeleteMetrics`); istekler `sample2/http-examples.http`. Aynı dosyada platform event (`TestPlatformEventController`, `SamplePlatformEventHandler`) ve Redis kilit (`TestRedis/AcquireLock`, `ReleaseLock`) örnekleri de var.
- **Unit testler**: `tests/BiUM.Tests/Database/DatabaseConfigurationExtensionsTests`, `Database/Native/NativeDatabaseRegistrationTests` (kayıt, migration filtreleme, idempotency, tip/bağlantı doğrulaması), `Database/Native/NpgsqlNativeDatabaseSqlTests` (merge/COPY/partition SQL üretimi, partition adı parse, tip eşlemesi). PostgreSQL gerektiren davranış (COPY, migration çalıştırma) sample2 üzerinden doğrulanır.
- BiApp.EnergyTracking: `Databases:Readings` (`energy_tracking_readings`), `ReadingRowMerge`, `LatestReadingsQuery`, günlük/aylık/yıllık partition setleri — ayrıntı: [BiApp.EnergyTracking Agents.Readings.md](../BiApp.EnergyTracking/Agents.Readings.md).

## 6. AI ajanları için

Native katmanın API’si, migration geçmiş tablosu veya desteklenen tipler değişirse bu dosya ve [AGENTS.md](AGENTS.md) güncellenmelidir.