# Agents.Crud.md — Domain CRUD & Runtime Model (BiUM)

Bu belge, BiUM içindeki **DomainCrud** (tanım/metadata) ile **runtime CRUD** (kodla veri işlemleri) ayrımını ve kiracı (tenant) semantiğini tek yerde tanımlar. Ürün kuralları ile kütüphane davranışı ayrı başlıklarda verilmiştir.

## 1. İki düzlem

| Düzlem | Amaç | Ana API yüzeyi |
|--------|------|----------------|
| **Tanım (metadata)** | `DomainCrud` oluşturma, kolonlar, çeviri, yayın (`PublishDomainCrud`), DDL ile tablo üretimi | `SaveDomainCrudAsync`, `DeleteDomainCrudAsync`, `GetDomainCrud*`, `GetDomainCrudsAsync`, `PublishDomainCrudAsync` |
| **Çalışma zamanı (data)** | Yayınlanmış `code` ile satır insert/update/delete/list | `CrudController` → `SaveAsync`, `SavePartialAsync`, `DeleteAsync`, `Get` (`ICrudService`) |

**Kısmi güncelleme (HTTP):** `CrudController` — `POST {code}/{partialCode}` (gövdede `Id`). Telafi: `AddSpecializedServices` ile eklenen global `CompensatableApiActionFilter` (telafiye uygun `code` ve gelen istekte boş oturumda yerel `CompensationSessionId`). Ayrıntı: [Agents.Compensation.md](Agents.Compensation.md).

## 2. Şema ve tablo (tanımdan)

- `code` ile çözülen **`DomainCrudVersion`**, fiziksel konumu **`ResolveSchema(ApplicationId, TenantId)`** ile belirler; buradaki `ApplicationId` ve `TenantId` **tanımın** (`DomainCrud` / versiyon) alanlarıdır.
- Bu, “tablo hangi uygulama ve tanım sahibi kiracının şemasında” sorusunun cevabıdır; **iş uygulamasının (app) tenant tabanlı modeli** ile uyumludur.

## 3. Satır `TENANT_ID` (çalışma anı — CorrelationContext)

- Dinamik tabloya **yazılan / güncellenen** satırdaki **`TENANT_ID`** kolonu, **tanımın** kiracısından değil, **`CorrelationContext.TenantId`** üzerinden doldurulur (ör. tanımı yapan “platform” kiracısı, uygulamayı kullanan **uç kiracı** kaydını ayırt etmek için).
- Amaç: tenant bazlı bir app içinde “bu kayıt hangi kiracıya ait?” bilgisinin satırda taşınması (**tenant isolation** veri düzleminde).

## 4. Mikroservis ve `code` benzersizliği

- CRUD veri tabloları **her mikroservisin kendi veritabanında** tutulduğu varsayımıyla, **aynı MS içinde `code` tek bir yayınlanmış tanıma** karşılık gelmelidir (metadata çakışması olmaması).
- `GetVersionByCodeAsync` yalnızca `code` ile en güncel versiyonu çözer; **MS başına tek DB** ile bu model tutarlıdır.

## 5. Tanım düzlemi — kiracı kuralları (`Ids.Customer.System.Id`)

System kiracı kimliği: `Ids.Customer.System.Id` (`BiUM.Core.Constants.Ids.Customer.System`). Okuma filtresi, BiApp.Configuration’daki `ServiceRepository` örüntüsüyle uyumludur: `TenantId == System || CorrelationContext.TenantId == null || TenantId == CorrelationContext.TenantId`.

- **Görünürlük (get / list / code ile get)**: Yukarıdaki filtreye uyan `DomainCrud` kayıtları döner; diğer kiracıya ait özel tanımlar görünmez.
- **Yazma (save güncelleme, silme, publish)**: `CorrelationContext.TenantId == System` **veya** `TenantId` bağlamda **yok** (null) ise tüm tanımlarda değişiklik yapılabilir; aksi halde yalnızca **`DomainCrud.TenantId == CorrelationContext.TenantId`** olan kayıtlar güncellenir / silinir / yayınlanır. System’a ait tanımlar (`TenantId == Ids.Customer.System.Id`) normal kiracı bağlamında **değiştirilemez**.
- **Yeni tanım (save insert)**: System bağlamı veya `TenantId` yok (null) ise oluşturma serbest; aksi halde bağlamda geçerli bir kiracı (`Guid.Empty` değil) gerekir. Yeni satırın `TenantId` değeri `CorrelationContext.TenantId ?? Guid.Empty` ile atanır.

Hata kodu (yetki yok): `crud_definition_access_denied`.

## 6. BiUM (`CrudService`) — tanım vs runtime

**Tanım düzlemi (`CrudService.Definition.cs`):**

- `GetDomainCrudAsync`, `GetDomainCrudByCodeAsync`, `GetDomainCrudsAsync`: okuma filtresi yukarıdaki System / kiracı kuralına göre uygulanır.
- `SaveDomainCrudAsync`, `DeleteDomainCrudAsync`, `PublishDomainCrudAsync`: mutasyon kuralları yukarıdaki gibi uygulanır.

**BiApp.Configuration servis kataloğu (dinamik CRUD):** `PublishDomainCrudAsync` sırasında `SaveCrudServicesAsync`, Configuration’daki `SaveCrudServices` komutunu çağırır. Bu komut, ilgili mikroservis için standart CRUD yüzeyini katalogda temsil eden servisleri yazar/günceller: **`Save`**, **`Delete`**, **`Get`**, **`GetList`** (BiUM `CrudController` ile uyumlu action adları ve route şablonları). Tanımda **partial update** kodları varsa, her kod için ek bir **`SavePartial`** `POST` servisi üretilir (`/api/base/Crud/SavePartial/{code}/{partialCode}`; gövde alanları servis parametreleriyle eşlenir). Komutta artık yer almayan partial kodlarına ait `Dynamic_Crud-{code}-SavePartial-*` adlı servisler **pasifleştirilir** (`Active = false`).

**Runtime düzlemi (aynı serviste, tanım dışı):**

- Tanım yayınında DDL, `SaveCrudServicesAsync` ile API kaydı, versiyonlama.
- `code` → şema/tablo, SQL ile CRUD; insert’te satır `TENANT_ID` = `CorrelationContext.TenantId`.
- Kısmen telafi (compensation) ve `C_STATUS` ile uyumlu okuma/yazma (ilgili bayraklar açıksa).

**Hâlâ host/policy ile tamamlanması beklenen (runtime):**

- İstek başına “bu `code` tanımı bu çağrı için kullanılabilir mi?” (API key, imza, ek policy) gateway veya özel middleware ile bağlanır.
- `GetListAsync` (dinamik tablo listesi) varsayılan olarak satır `tenantId` ile otomatik kısıtlama yapmaz; çok kiracılı tek tabloda filtre **query parametresi** veya politika ile eklenmelidir.

## 7. İlgili kod konumları

- Tanım: `BiUM.Specialized/Services/Crud/CrudService.Definition.cs`
- Runtime SQL: `CrudService.ddl.cs` (`GetVersionByCodeAsync`, `CreateInternalAsync`, `ResolveSchema`), `CrudService.Api.cs`
- HTTP: `BiUM.Specialized/Common/API/CrudController.cs`

## 8. AI ajanları için

Bu belge değiştiğinde ürün tarafında davranış eşleşmesi gerekiyorsa host servislerindeki policy/filtreler gözden geçirilmelidir.
