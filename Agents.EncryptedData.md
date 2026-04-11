# Agents.EncryptedData.md — Kalıcı alan şifreleme / koruma (BiUM)

Bu belge, EF Core üzerinde **`[EncryptedData]`** ile işaretli `string` özelliklerin veritabanına yazılırken korunmasını özetler.

## 1. Öznitelik

- **`EncryptedDataAttribute`** (`BiUM.Core/Common/Attributes/EncryptedDataAttribute.cs`)
- **`Reversible`** (varsayılan `false`): `true` ise değer geri çözülebilir şekilde korunur; `EncryptionHelper.Protect` / `Unprotect` bayrağına bağlanır.

## 2. EF Core dönüştürücü

- **`EncryptedDataValueConverter`** (`BiUM.Infrastructure/Persistence/ValueConverters/EncryptedDataValueConverter.cs`): `string?` → `string?`; `EncryptionHelper.Protect` (persist), `Unprotect` (okuma).

## 3. Model oluşturma

- **`ModelBuilderEncryptedDataExtensions.ApplyEncryptedDataConversion`** (`BiUM.Infrastructure/Persistence/Extensions/ModelBuilderEncryptedDataExtensions.cs`):
  - Tüm entity özelliklerinde `[EncryptedData]` aranır.
  - Yalnızca **`string`** özellikler desteklenir; aksi halde `InvalidOperationException`.
  - `encryptionKey` boş olamaz (extension metodu için).

## 4. `BaseDbContext` entegrasyonu

- **`BiUM.Specialized/Database/BaseDbContext.cs`** — `OnModelCreating` içinde `BiAppOptions.EncryptionKey` **doluysa** `modelBuilder.ApplyEncryptedDataConversion(BiAppOptions.EncryptionKey)` çağrılır.
- Anahtar yoksa dönüştürücü uygulanmaz; özellikler düz metin kalır.

## 5. Yapılandırma

- Anahtar: **`BiAppOptions.EncryptionKey`** (`BiUM.Core/Common/Configs/BiAppOptions.cs` — AGENTS.md’de belirtildiği gibi).
- Uygulama ctor’larında `BiAppOptions` + `IServiceProvider` deseni, generator notlarıyla uyumludur (şifreleme anahtarının DbContext’e ulaşması).

## 6. Diğer yardımcılar

- **`EncryptionHelper`** (`BiUM.Core/Common/Utils/EncryptionHelper.cs`): Encrypt/Decrypt, Hash/Verify, Protect/Unprotect — uygulama kodu (login, export vb.) doğrudan da kullanabilir.

## 7. AI ajanları için

Desteklenen türler yalnızca `string`; yeni tür veya anahtar rotasyonu stratejisi eklenirse bu dosya ve `AGENTS.md` § EncryptedData güncellenmelidir.
