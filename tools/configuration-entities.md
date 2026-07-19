# BiApp — Entity Referansı

> **Kapsam:** Standart (BiUM) · Configuration · Bpmn · Authentication · Parameters

---

# DB Sorgulama Rehberi

## Kurulum (bir kerelik)

```bash
dotnet tool install -g dotnet-script
```

## Kullanım

```bash
dotnet-script tools/query-db.csx "<connection-string>" "<SELECT sorgusu>"
```

**Örnek:**
```bash
dotnet-script tools/query-db.csx \
  "Host=dev.shared.db.postgres.bidyno.com;Port=30543;Database=parameters;Username=postgres;Password=password;" \
  "SELECT \"ID\", \"NAME\", \"SERVICE_ID\" FROM dbo.\"PARAMETER\" WHERE \"DELETED\" = false LIMIT 10"
```

## PostgreSQL Konvansiyonları

Bu projelerde PostgreSQL'de aşağıdaki kurallar geçerlidir. Sorgu yazarken bunlara dikkat et:

### Schema
- Tüm tablolar `dbo` schema'sındadır (`public` değil)
- Syntax: `dbo."TABLO_ADI"`

### Tablo adları
- Tamamen **BÜYÜK HARF** ve snake_case: `PARAMETER`, `SERVICE`, `CRUD_COLUMN`
- Her zaman çift tırnak içinde kullan: `dbo."PARAMETER"` ✓ / `dbo.PARAMETER` ✗

### Kolon adları
- Tamamen **BÜYÜK HARF** ve snake_case: `ID`, `NAME`, `SERVICE_ID`, `DELETED`, `TENANT_ID`
- C# entity property adları PascalCase (`ServiceId`) → DB kolonu `SERVICE_ID`
- Her zaman çift tırnak içinde kullan: `"SERVICE_ID"` ✓ / `SERVICE_ID` ✗ (reserved word çakışması riski)

### Boolean filtreler
```sql
WHERE "DELETED" = false    -- ✓ doğru
WHERE "DELETED" = 'false'  -- ✗ yanlış (string karşılaştırması)
WHERE "DELETED" = FALSE    -- ✓ çalışır ama küçük harf tercih edilir
```

### Soft delete
- **Her sorguda** `WHERE "DELETED" = false` ekle, yoksa silinmiş kayıtlar da gelir
- Multi-tenant tablolarda genellikle `"TENANT_ID"` de filtrele

### Database adları (bilinen)
| Proje | Database adı |
|-------|-------------|
| BiApp.Parameters | `parameters` |
| BiApp.Configuration | `configuration` |
| BiApp.Authentication | `authentication` |
| BiApp.Bpmn | `bpmn` |
| BiApp.Accounting | `accounting` |
| Diğerleri | genellikle proje adının küçük harfi |

### C# → DB adı dönüşümü
| C# (entity) | PostgreSQL |
|-------------|-----------|
| `ServiceId` | `SERVICE_ID` |
| `MicroserviceId` | `MICROSERVICE_ID` |
| `CreatedTime` | `CREATED_TIME` |
| `IsDefault` | `IS_DEFAULT` |
| `TenantId` | `TENANT_ID` |

### Standart tablolar için prefix
Standart BiUM tabloları `__` prefix'i ile başlar:
```sql
dbo."__CRUD"
dbo."__TRANSLATION"
dbo."__BOLT_STATUS"
dbo."__COMPENSATION_SNAPSHOT"
```

### Örnek sorgular

**Tüm tabloları listele:**
```sql
SELECT table_schema, table_name
FROM information_schema.tables
WHERE table_type = 'BASE TABLE'
  AND table_schema NOT IN ('pg_catalog','information_schema')
ORDER BY table_schema, table_name
```

**Bir tablonun kolonlarını listele:**
```sql
SELECT column_name, data_type, is_nullable
FROM information_schema.columns
WHERE table_schema = 'dbo' AND table_name = 'PARAMETER'
ORDER BY ordinal_position
```

**Temel SELECT şablonu:**
```sql
SELECT "ID", "NAME", "TENANT_ID"
FROM dbo."PARAMETER"
WHERE "DELETED" = false
  AND "TENANT_ID" = '<tenant-guid>'
ORDER BY "NAME"
LIMIT 100
```

---

# BiUM — Standart Tablolar (Her MS'e Otomatik Eklenir)

> Her microservice DbContext'i `BaseDbContext`'ten miras alır. Aşağıdaki tüm tablo ve entity'ler otomatik olarak her DB'ye gelir.

## Standart Entity'ler

### __COMPENSATION_SNAPSHOT
**Class:** `DomainCompensationSnapshot` · **Base:** `TenantBaseEntity`  
**Amaç:** Distributed transaction / saga pattern için eski/yeni veri snapshot'ı tutar.

| Alan | Tip | Zorunlu |
|------|-----|---------|
| EntityName | string | ✓ |
| ApplicationId | Guid? | - |
| SnapshotTableName | string? | - |
| EntityClrTypeName | string? | - |
| EntityId | Guid | ✓ |
| OperationType | int | ✓ |
| CompensationSessionId | Guid | ✓ |
| OldDataJson | string? (JSON) | - |
| NewDataJson | string? (JSON) | - |
| Version | int | ✓ |
| State | int | ✓ |
| ExpireAt | DateTime? | - |
| ProcessedAt | DateTime? | - |

---

### __CRUD
**Class:** `DomainCrud` · **Base:** `TenantBaseEntity`  
**Amaç:** Her MS'deki entity'lerin CRUD metadata'sını tanımlar (field, kolon, tablo bilgisi).

| Alan | Tip | Zorunlu |
|------|-----|---------|
| ApplicationId | Guid | ✓ |
| MicroserviceId | Guid | ✓ |
| Name | string | ✓ |
| Code | string | ✓ |
| TableName | string | ✓ |
| Compensatible | bool | ✓ |

**İlişkiler:** DomainCrudColumns (1:N), DomainCrudTranslations (1:N), DomainCrudPartialUpdates (1:N)

---

### __CRUD_COLUMN
**Class:** `DomainCrudColumn` · **Base:** `BaseEntity`

| Alan | Tip | Zorunlu |
|------|-----|---------|
| CrudId | Guid | ✓ |
| PropertyName | string | ✓ |
| ColumnName | string | ✓ |
| FieldId | Guid | ✓ |
| DataTypeId | Guid | ✓ |
| MaxLength | int? | - |
| SortOrder | int | ✓ |

**İlişkiler:** DomainCrud (FK)

---

### __CRUD_TRANSLATION
**Class:** `DomainCrudTranslation` · **Base:** `TranslationBaseEntity`  
**İlişkiler:** DomainCrud (FK via RecordId)

---

### __CRUD_PARTIAL_UPDATE
**Class:** `DomainCrudPartialUpdate` · **Base:** `TenantBaseEntity`

| Alan | Tip | Zorunlu |
|------|-----|---------|
| CrudId | Guid | ✓ |
| Code | string | ✓ |
| Name | string? | - |

**Index:** Unique (CrudId, Code)  
**İlişkiler:** DomainCrud (FK), Columns (1:N)

---

### __CRUD_PARTIAL_UPDATE_COLUMN
**Class:** `DomainCrudPartialUpdateColumn` · **Base:** `BaseEntity`

| Alan | Tip | Zorunlu |
|------|-----|---------|
| PartialUpdateId | Guid | ✓ |
| CrudColumnId | Guid | ✓ |

**İlişkiler:** DomainCrudPartialUpdate (FK), DomainCrudColumn (FK)

---

### __CRUD_VERSION
**Class:** `DomainCrudVersion` · **Base:** `TenantBaseEntity`  
**Amaç:** CRUD metadata'nın versiyonlanmış snapshot'ı.

| Alan | Tip | Zorunlu |
|------|-----|---------|
| ApplicationId | Guid | ✓ |
| CrudId | Guid | ✓ |
| TableName | string | ✓ |
| Version | int | ✓ |

**İlişkiler:** DomainCrudVersionColumns (1:N), DomainCrudVersionPartialUpdates (1:N)

---

### __CRUD_VERSION_COLUMN
**Class:** `DomainCrudVersionColumn` · **Base:** `BaseEntity`

| Alan | Tip | Zorunlu |
|------|-----|---------|
| CrudVersionId | Guid | ✓ |
| PropertyName | string | ✓ |
| ColumnName | string | ✓ |
| FieldId | Guid | ✓ |
| DataTypeId | Guid | ✓ |
| MaxLength | int? | - |
| SortOrder | int | ✓ |

**İlişkiler:** DomainCrudVersion (FK)

---

### __CRUD_VERSION_PARTIAL_UPDATE
**Class:** `DomainCrudVersionPartialUpdate` · **Base:** `TenantBaseEntity`

| Alan | Tip | Zorunlu |
|------|-----|---------|
| CrudVersionId | Guid | ✓ |
| Code | string | ✓ |
| Name | string? | - |

**Index:** Unique (CrudVersionId, Code)

---

### __CRUD_VERSION_PARTIAL_UPDATE_COLUMN
**Class:** `DomainCrudVersionPartialUpdateColumn` · **Base:** `BaseEntity`

| Alan | Tip | Zorunlu |
|------|-----|---------|
| VersionPartialUpdateId | Guid | ✓ |
| CrudVersionColumnId | Guid | ✓ |

**İlişkiler:** DomainCrudVersionPartialUpdate (FK), DomainCrudVersionColumn (FK)

---

### __TRANSLATION
**Class:** `DomainTranslation` · **Base:** `BaseEntity`  
**Amaç:** Her MS'e ait çeviri anahtarlarını tutar.

| Alan | Tip | Zorunlu |
|------|-----|---------|
| ApplicationId | Guid | ✓ |
| Code | string | ✓ |

**İlişkiler:** DomainTranslationDetails (1:N)

---

### __TRANSLATION_DETAIL
**Class:** `DomainTranslationDetail` · **Base:** `BaseEntity`

| Alan | Tip | Zorunlu |
|------|-----|---------|
| TranslationId | Guid | ✓ |
| Text | string | ✓ |
| LanguageId | Guid | ✓ |

**İlişkiler:** DomainTranslation (FK)

---

### __BOLT_STATUS
**Class:** `BoltStatus` · **Base:** `BaseEntity`  
**Amaç:** Bolt (event sourcing / compensating transaction) işlemlerinin durumunu takip eder.  
**Not:** `[Auditable(false)]`

| Alan | Tip | Zorunlu |
|------|-----|---------|
| LastTransactionId | Guid? | - |
| Error | string? | - |

---

### __BOLT_TRANSACTION
**Class:** `BoltTransaction` · **Base:** `BaseEntity`  
**Amaç:** Bolt compensating transaction log'u — her işlemin hangi tablo + ID'leri etkilediğini kaydeder.  
**Not:** `[Auditable(false)]`

| Alan | Tip | Zorunlu |
|------|-----|---------|
| TableName | string | ✓ |
| Ids | string? | - (`;` ile ayrılmış ID listesi) |
| Delete | bool | ✓ |
| SortOrder | int | ✓ |

---

## Standart Tablo İlişki Haritası

```
DomainCrud ──< DomainCrudColumn ──< DomainCrudPartialUpdateColumn
           ──< DomainCrudTranslation
           ──< DomainCrudPartialUpdate ──< DomainCrudPartialUpdateColumn

DomainCrudVersion ──< DomainCrudVersionColumn ──< DomainCrudVersionPartialUpdateColumn
                  ──< DomainCrudVersionPartialUpdate ──< DomainCrudVersionPartialUpdateColumn

DomainTranslation ──< DomainTranslationDetail

BoltStatus   (bağımsız)
BoltTransaction  (bağımsız)
DomainCompensationSnapshot  (bağımsız)
```

## Otomatik Query Filtreleri (BaseDbContext)

| Filtre | Kapsam |
|--------|--------|
| `Deleted == false` | `IBaseEntity` miras alan tüm entity'ler |
| Compensation session filtresi | `ICompensation` uygulayan entity'ler |
| Readable compensation filtresi | `IReadableCompensation` uygulayan entity'ler |

---

---

# BiApp.Configuration — Entity Referansı

## Base Sınıflar (BiUM.Infrastructure)

| Sınıf | Alanlar |
|-------|---------|
| `BaseEntity` | `Id` (Guid), `CorrelationId`, `Active`, `Deleted`, `Created`, `CreatedTime`, `CreatedBy`, `Updated`, `UpdatedTime`, `UpdatedBy`, `Test` |
| `TenantBaseEntity` | BaseEntity + `TenantId` |
| `TranslationBaseEntity` | `Id`, `CorrelationId`, `RecordId`, `Column`, `LanguageId`, `Translation` |

---

## Entityler

### AccessPoint
**Tablo:** `AccessPoints` · **Base:** `TenantBaseEntity`

| Alan | Tip | Zorunlu |
|------|-----|---------|
| ApplicationId | Guid? | - |
| WorkgroupId | Guid? | - |
| RoleId | Guid? | - |
| CustomerId | Guid? | - |
| ApplicationClientId | Guid? | - |

---

### AccessRight
**Tablo:** `AccessRights` · **Base:** `TenantBaseEntity`

| Alan | Tip | Zorunlu |
|------|-----|---------|
| ApplicationId | Guid? | - |
| AccessPointId | Guid? | - |
| Type | Guid | ✓ |
| ServiceId | Guid? | - |
| ResourceId | Guid? | - |
| ActionId | Guid? | - |

**İlişkiler:** Application, AccessPoint, Service, Resource, Action

---

### Action
**Tablo:** `Actions` · **Base:** `BaseEntity`

| Alan | Tip | Zorunlu |
|------|-----|---------|
| Name | string | ✓ |
| IconId | Guid | ✓ |
| BpmnAction | bool | default: false |

**İlişkiler:** Icon (FK), ActionTranslations (1:N)

---

### ActionTranslation
**Base:** `TranslationBaseEntity`  
**İlişkiler:** Action (FK via RecordId)

---

### Application
**Tablo:** `Applications` · **Base:** `TenantBaseEntity`

| Alan | Tip | Zorunlu |
|------|-----|---------|
| Name | string | ✓ |
| Description | string | ✓ |
| ParentId | Guid? | - |
| LayoutType | Guid | ✓ |
| Domain | string? | - |
| EnvironmentSetup | bool? | - |
| ShowInApplication | bool | default: false |
| Published | bool | default: false |
| Version | int | default: 0 |

**İlişkiler:** Parent/Children (self-ref), Resources (1:N), ApplicationTranslations (1:N)

---

### ApplicationTranslation
**Base:** `TranslationBaseEntity`  
**İlişkiler:** Application (FK via RecordId)

---

### ApplicationClient
**Tablo:** `ApplicationClients` · **Base:** `TenantBaseEntity`

| Alan | Tip | Zorunlu |
|------|-----|---------|
| ApplicationId | Guid | ✓ |
| ClientId | Guid | ✓ (unique) |
| ClientSecret | string | ✓ (encrypted) |
| IsWebApp | bool | default: false |
| Version | int | default: 0 |

---

### ApplicationLanguage
**Tablo:** `ApplicationLanguages` · **Base:** `TenantBaseEntity`  
**Generator:** Save, Delete, Get, GetList, EntityEvents

| Alan | Tip | Zorunlu |
|------|-----|---------|
| ApplicationId | Guid | ✓ |
| LanguageId | Guid | ✓ |
| SortOrder | int | ✓ |

**İlişkiler:** Application (FK), Language (FK)

---

### ApplicationTheme
**Tablo:** `ApplicationThemes` · **Base:** `TenantBaseEntity`

| Alan | Tip | Zorunlu |
|------|-----|---------|
| ApplicationId | Guid | ✓ |
| CustomerId | Guid? | - |
| Name | string | ✓ |
| IsDefault | bool | default: false |
| Properties | string? (JSON) | - |
| Version | int | default: 0 |

---

### Channel
**Tablo:** `Channels` · **Base:** `BaseEntity`

| Alan | Tip | Zorunlu |
|------|-----|---------|
| Name | string | ✓ |
| Description | string | ✓ |

**İlişkiler:** ChannelTranslations (1:N)

---

### ChannelTranslation
**Base:** `TranslationBaseEntity`  
**İlişkiler:** Channel (FK via RecordId)

---

### Component
**Tablo:** `Components` · **Base:** `BaseEntity`

| Alan | Tip | Zorunlu |
|------|-----|---------|
| Name | string | ✓ |
| Code | string | ✓ |
| Type | Guid | ✓ |
| UseInDesigner | bool | default: true |
| HasAction | bool | default: false |

**İlişkiler:** ComponentProperties (1:N), ResourceComponentMaps (1:N)

---

### ComponentProperty
**Tablo:** `ComponentProperties` · **Base:** `BaseEntity`

| Alan | Tip | Zorunlu |
|------|-----|---------|
| ComponentId | Guid | ✓ |
| Name | string | ✓ |
| Value | string? | - |
| Object | string? | - |
| Selection | string? | - |
| Enums | string? | - |
| DataType | Guid | ✓ |
| PropertyType | Guid | ✓ |

**İlişkiler:** Component (FK)

---

### DataType
**Tablo:** `DataTypes` · **Base:** `BaseEntity`

| Alan | Tip | Zorunlu |
|------|-----|---------|
| Name | string | ✓ |
| Property | string | ✓ |
| ParentId | Guid? | - |
| OwnDataTypeId | Guid? | - |
| FieldId | Guid? | - |

**İlişkiler:** Parent/DataTypes (self-ref), Field (FK)

---

### Field
**Tablo:** `Fields` · **Base:** `BaseEntity`

| Alan | Tip | Zorunlu |
|------|-----|---------|
| Name | string | ✓ |
| Code | string | ✓ |
| ComponentId | Guid | ✓ |
| DataTypeId | Guid | ✓ |
| Array | bool | default: false |
| Nullable | bool | default: false |

**İlişkiler:** Component (FK), DataType (FK), ResourceFieldMaps (1:N), FieldProperties (1:N), FieldTranslations (1:N)

---

### FieldTranslation
**Base:** `TranslationBaseEntity`  
**İlişkiler:** Field (FK via RecordId)

---

### FieldProperty
**Tablo:** `FieldProperties` · **Base:** `BaseEntity`

| Alan | Tip | Zorunlu |
|------|-----|---------|
| FieldId | Guid | ✓ |
| Value | string | ✓ |
| ComponentPropertyId | Guid | ✓ |

**İlişkiler:** Field (FK), ComponentProperty (FK)

---

### Icon
**Tablo:** `Icons` · **Base:** `BaseEntity`

| Alan | Tip | Zorunlu |
|------|-----|---------|
| Name | string | ✓ |

**İlişkiler:** Actions (1:N), Resources (1:N)

---

### Interaction
**Tablo:** `Interactions` · **Base:** `TenantBaseEntity`

| Alan | Tip | Zorunlu |
|------|-----|---------|
| ResourceId | Guid | ✓ |
| FlowId | Guid? | - |
| FlowRouteId | string? | - |
| Action | string | ✓ |
| Name | string | ✓ |
| Content | string (JSON) | ✓ |

**İlişkiler:** Resource (FK)

---

### Language
**Tablo:** `Languages` · **Base:** `BaseEntity`

| Alan | Tip | Zorunlu |
|------|-----|---------|
| Code | string | ✓ |
| Name | string | ✓ |
| Rtl | bool | default: false |
| SortOrder | int | ✓ |

**İlişkiler:** TranslationDetails (1:N)

---

### Microservice
**Tablo:** `Microservices` · **Base:** `BaseEntity`

| Alan | Tip | Zorunlu |
|------|-----|---------|
| Name | string | ✓ |
| Description | string | ✓ |
| RootPath | string | ✓ |

**İlişkiler:** Resources (1:N), Services (1:N), Translations (1:N)

---

### Resource
**Tablo:** `Resources` · **Base:** `TenantBaseEntity`

| Alan | Tip | Zorunlu |
|------|-----|---------|
| ApplicationId | Guid | ✓ |
| MicroserviceId | Guid | ✓ |
| Name | string | ✓ |
| Type | Guid | ✓ |
| Path | string? | - |
| ParentId | Guid? | - |
| FlowId | Guid? | - |
| LayoutId | Guid? | - |
| IconId | Guid? | - |
| BasedOnResourceId | Guid? | - |
| LoginRequired | bool | default: false |
| IsMultipleAllowed | bool | default: false |
| ShowInMenu | bool | default: false |
| SortOrder | int | ✓ |
| MainPage | bool | default: false |
| Published | bool | default: false |
| NeedsPublish | bool | default: false |

**İlişkiler:** Application (FK), Icon (FK), Microservice (FK), Parent/Layout/BasedOnResource (self-ref), ResourceTranslations, ResourceActions, ResourceComponentMaps, Interactions (1:N)

---

### ResourceTranslation
**Base:** `TranslationBaseEntity`  
**İlişkiler:** Resource (FK via RecordId)

---

### ResourceAction
**Tablo:** `ResourceActions` · **Base:** `BaseEntity`

| Alan | Tip | Zorunlu |
|------|-----|---------|
| ResourceId | Guid | ✓ |
| ActionId | Guid | ✓ |
| AfterActionComplete | string? | - |
| HiddenExpression | string? | - |
| OwnerFieldId | Guid? | - |
| TargetFieldId | Guid? | - |
| Type | string | ✓ |
| OperationType | int | ✓ |
| Properties | string? | - |
| Order | int | ✓ |
| Visible | bool | default: false |

**İlişkiler:** Resource (FK), Action (FK), OwnerField/TargetField (FK to Field)

---

### ResourceComponentMap
**Tablo:** `ResourceComponentMaps` · **Base:** `BaseEntity`

| Alan | Tip | Zorunlu |
|------|-----|---------|
| ResourceId | Guid | ✓ |
| ComponentId | Guid | ✓ |
| Name | string | ✓ |
| ParentId | Guid? | - |
| Order | int | ✓ |

**İlişkiler:** Resource (FK), Component (FK), Parent (self-ref), ResourceFieldMap (1:1)

---

### ResourceFieldMap
**Tablo:** `ResourceFieldMaps` · **Base:** `BaseEntity`

| Alan | Tip | Zorunlu |
|------|-----|---------|
| ResourceComponentMapId | Guid | ✓ |
| FieldId | Guid | ✓ |

**İlişkiler:** ResourceComponentMap (FK), Field (FK), ResourceFieldMapProperties (1:N)

---

### ResourceFieldMapProperty
**Tablo:** `ResourceFieldMapProperties` · **Base:** `BaseEntity`

| Alan | Tip | Zorunlu |
|------|-----|---------|
| ResourceFieldMapId | Guid | ✓ |
| ComponentPropertyId | Guid | ✓ |
| Value | string | ✓ |

**İlişkiler:** ResourceFieldMap (FK), ComponentProperty (FK)

---

### Service
**Tablo:** `Services` · **Base:** `TenantBaseEntity`

| Alan | Tip | Zorunlu |
|------|-----|---------|
| ApplicationId | Guid | ✓ |
| MicroserviceId | Guid | ✓ |
| Type | Guid | ✓ |
| Name | string | ✓ |
| Url | string | ✓ |
| HttpType | Guid | ✓ |
| TimeoutMs | int? | - |
| ServiceAuthenticationId | Guid? | - |

**İlişkiler:** Application (FK), Microservice (FK), Authentication (FK), ServiceParameters (1:N)

---

### ServiceAuthentication
**Tablo:** `ServiceAuthentications` · **Base:** `TenantBaseEntity`

| Alan | Tip | Zorunlu |
|------|-----|---------|
| ApplicationId | Guid | ✓ |
| AuthType | Guid | ✓ |
| TokenUrl | string? | - |
| Username | string? | - |
| Password | string? | - |
| ClientId | string? | - |
| ClientSecret | string? | - |
| ApiKey | string? | - |
| ApiKeyHeaderName | string? | - |
| Audience | string? | - |
| Scope | string? | - |
| CustomHeadersJson | string? (JSON) | - |

---

### ServiceParameter
**Tablo:** `ServiceParameters` · **Base:** `BaseEntity`

| Alan | Tip | Zorunlu |
|------|-----|---------|
| ServiceId | Guid | ✓ |
| DirectionType | Guid | ✓ |
| Property | string | ✓ |
| FieldId | Guid | ✓ |

**İlişkiler:** Service (FK), Field (FK)

---

### Translation
**Tablo:** `Translations` · **Base:** `BaseEntity`

| Alan | Tip | Zorunlu |
|------|-----|---------|
| ApplicationId | Guid | ✓ |
| MicroserviceId | Guid | ✓ |
| Code | string | ✓ |
| UsageType | string | ✓ |
| Type | string | ✓ |

**İlişkiler:** Application (FK), Microservice (FK), TranslationDetails (1:N)

---

### TranslationDetail
**Tablo:** `TranslationDetails` · **Base:** `BaseEntity`

| Alan | Tip | Zorunlu |
|------|-----|---------|
| TranslationId | Guid | ✓ |
| Text | string | ✓ |
| LanguageId | Guid | ✓ |

**İlişkiler:** Language (FK), Translation (FK)

---

### Validation
**Tablo:** `Validations` · **Base:** `TenantBaseEntity`

| Alan | Tip | Zorunlu |
|------|-----|---------|
| ResourceId | Guid | ✓ |
| FlowId | Guid? | - |
| FlowRouteId | string? | - |
| ActionId | Guid | ✓ |
| Name | string | ✓ |
| Content | string (JSON) | ✓ |

**İlişkiler:** Resource (FK), Action (FK)

---

### ResourcePublished
**Tablo:** `ResourcePublisheds` · **Base:** `TenantBaseEntity`

| Alan | Tip | Zorunlu |
|------|-----|---------|
| ResourceId | Guid | ✓ |
| ApplicationId | Guid | ✓ |
| MicroserviceId | Guid | ✓ |
| Version | int | ✓ |
| Default | bool | default: false |
| Name | string | ✓ |
| Type | Guid | ✓ |
| Path | string? | - |
| ParentId | Guid? | - |
| FlowId | Guid? | - |
| LayoutId | Guid? | - |
| IconId | Guid? | - |
| IconName | string? | - |
| LoginRequired | bool | default: false |
| IsMultipleAllowed | bool | default: false |
| ShowInMenu | bool | default: false |
| MainPage | bool | default: false |
| SortOrder | int | ✓ |
| ScreenPropertiesJson | string? (JSON) | - |
| PublishedAt | DateTime | ✓ |
| PublishedBy | Guid | ✓ |

**İlişkiler:** Translations, Actions, Interactions, Validations (1:N — snapshot)

---

### ResourcePublishedAction / Interaction / Validation
**Base:** `BaseEntity`

| Alan | Tip | Zorunlu |
|------|-----|---------|
| ResourcePublishedId | Guid | ✓ |
| Json | string (JSON) | ✓ |

---

### ResourcePublishedTranslation
**Base:** `BaseEntity`

| Alan | Tip | Zorunlu |
|------|-----|---------|
| ResourcePublishedId | Guid | ✓ |
| LanguageId | Guid | ✓ |
| Name | string | ✓ |

---

## İlişki Haritası

```
Application ──< Resource ──< ResourceAction >── Action ──> Icon
     │               │
     │               ├──< ResourceComponentMap ──> Component ──< ComponentProperty
     │               │         └── ResourceFieldMap ──> Field ──< FieldProperty
     │               │
     │               ├──< Interaction
     │               └──< Validation >── Action
     │
     ├──< Service ──< ServiceParameter >── Field
     │       └──> ServiceAuthentication
     │
     ├──< ApplicationLanguage >── Language ──< TranslationDetail ──> Translation
     ├──< ApplicationTheme
     └──< ApplicationClient

Microservice ──< Resource
            ──< Service
            ──< Translation ──< TranslationDetail

ResourcePublished ──< ResourcePublishedAction
                 ──< ResourcePublishedInteraction
                 ──< ResourcePublishedValidation
                 ──< ResourcePublishedTranslation

DataType ──< DataType (self-ref parent)
Resource ──< Resource (self-ref: Parent, Layout, BasedOnResource)
ResourceComponentMap ──< ResourceComponentMap (self-ref parent)
```

---

## Notlar (Configuration)

- **Multi-tenancy:** TenantBaseEntity miras alan entity'lerde `TenantId` ile izolasyon sağlanır.
- **Soft delete:** Tüm entity'lerde `Deleted` alanı; sorgularda `WHERE Deleted = false` ekle.
- **JSON kolonlar:** `Interaction.Content`, `Validation.Content`, `ResourcePublished.ScreenPropertiesJson`, `ResourcePublished*.Json`, `ServiceAuthentication.CustomHeadersJson`, `ApplicationTheme.Properties`
- **Şifreli alan:** `ApplicationClient.ClientSecret` — DB'de şifreli tutulur.
- **Snapshot pattern:** `ResourcePublished*` entity'leri, yayınlanan resource'un anlık görüntüsüdür; canlı `Resource` ile senkron değildir.

---

---

# BiApp.Bpmn — Entity Referansı

## Entityler (13)

### Flow
**Tablo:** `FLOW` · **Base:** `TenantBaseEntity`

| Alan | Tip | Zorunlu |
|------|-----|---------|
| ApplicationId | Guid | ✓ |
| MicroserviceId | Guid | ✓ |
| Name | string | ✓ |
| Description | string? | - |
| Wizard | bool | default: false |
| DraftFlowVersionId | Guid? | - |

**İlişkiler:** FlowVersions (1:N), FlowFields (1:N), FlowTranslations (1:N)

---

### FlowTranslation
**Base:** `TranslationBaseEntity`  
**İlişkiler:** Flow (FK via RecordId)

---

### FlowField
**Tablo:** `FLOW_FIELD` · **Base:** `BaseEntity`

| Alan | Tip | Zorunlu |
|------|-----|---------|
| FlowId | Guid | ✓ |
| FieldId | Guid | ✓ |

**İlişkiler:** Flow (FK)

---

### FlowVersion
**Tablo:** `FLOW_VERSION` · **Base:** `BaseEntity`

| Alan | Tip | Zorunlu |
|------|-----|---------|
| FlowId | Guid | ✓ |
| Version | int | ✓ |
| Content | string (JSON) | ✓ |
| Default | bool | ✓ |
| Published | bool | ✓ |

**İlişkiler:** Flow (FK), Instances (1:N), FlowRoutes/Routes (1:N), FlowRouteMaps/RouteMaps (1:N)

---

### Route
**Tablo:** `ROUTE` · **Base:** `BaseEntity`

| Alan | Tip | Zorunlu |
|------|-----|---------|
| FlowVersionId | Guid | ✓ |
| RouteId | string | ✓ |
| Type | string | ✓ |
| Data | string | ✓ |
| RelatedType | string? | - |
| RelatedId | Guid? | - |
| SystemApprove | bool | ✓ |

**İlişkiler:** FlowVersion (FK)

---

### Instance
**Tablo:** `INSTANCE` · **Base:** `TenantBaseEntity`

| Alan | Tip | Zorunlu |
|------|-----|---------|
| FlowVersionId | Guid | ✓ |
| ParentId | Guid? | - |
| StartRouteId | Guid? | - |
| StatusType | Guid | ✓ |
| CompensationSessionId | Guid? | - |

**İlişkiler:** FlowVersion (FK), Parent/Children (self-ref), StartRoute (FK to Route)

---

### InstanceActor
**Tablo:** `INSTANCE_ACTOR` · **Base:** `BaseEntity`

| Alan | Tip | Zorunlu |
|------|-----|---------|
| InstanceId | Guid | ✓ |
| CustomerId | Guid | ✓ |
| RouteId | Guid | ✓ |
| Type | string | ✓ |

**İlişkiler:** Instance (FK), Route (FK)

---

### InstanceData
**Tablo:** `INSTANCE_DATA` · **Base:** `BaseEntity`

| Alan | Tip | Zorunlu |
|------|-----|---------|
| InstanceId | Guid | ✓ |
| Data | string? | - |

**İlişkiler:** Instance (FK)

---

### InstanceRoute
**Tablo:** `INSTANCE_ROUTE` · **Base:** `BaseEntity`

| Alan | Tip | Zorunlu |
|------|-----|---------|
| InstanceId | Guid | ✓ |
| RouteId | Guid | ✓ |
| PreviousRouteId | Guid? | - |
| ActionId | Guid | ✓ |
| Default | bool | ✓ |
| RelatedId | Guid? | - |

**İlişkiler:** Instance (FK), Route (FK), PreviousRoute (FK to Route, nullable)

---

### RouteMap
**Tablo:** `ROUTE_MAP` · **Base:** `BaseEntity`

| Alan | Tip | Zorunlu |
|------|-----|---------|
| FlowVersionId | Guid | ✓ |
| SourceRouteId | Guid | ✓ |
| SourceRouteType | string | ✓ |
| TargetRouteId | Guid | ✓ |
| TargetRouteType | string | ✓ |
| ActionId | Guid | ✓ |
| Data | string? | - |

**İlişkiler:** FlowVersion (FK), SourceRoute/TargetRoute (FK to Route)

---

### FlowCompiledArtifact
**Tablo:** `FLOW_COMPILED_ARTIFACT` · **Base:** `BaseEntity`

| Alan | Tip | Zorunlu |
|------|-----|---------|
| FlowVersionId | Guid | ✓ (unique) |
| ContentHash | string | ✓ |
| AssemblyBytes | byte[] | ✓ |

**İlişkiler:** FlowVersion (FK)

---

### FlowPublishedFieldMap
**Tablo:** `FLOW_PUBLISHED_FIELD_MAP` · **Base:** `BaseEntity`

| Alan | Tip | Zorunlu |
|------|-----|---------|
| FlowVersionId | Guid | ✓ |
| FieldCode | string | ✓ |
| FieldId | Guid | ✓ |

**İlişkiler:** FlowVersion (FK)  
**Index:** Unique (FlowVersionId, FieldCode)

---

### UserAuthorizationMirror
**Tablo:** `USER_AUTHORIZATION_MIRROR` · **Base:** `BaseEntity`

| Alan | Tip | Zorunlu |
|------|-----|---------|
| ApplicationId | Guid | ✓ |
| TenantId | Guid | ✓ |
| UserId | Guid | ✓ |
| ResourceId | Guid | ✓ |
| ActionId | Guid | ✓ |

**İlişkiler:** Yok (mirror/cache tablosu)

---

## İlişki Haritası (Bpmn)

```
Flow ──< FlowVersion ──< Route
    │          ├──< RouteMap >── Route (source/target)
    │          ├──< Instance ──< InstanceActor >── Route
    │          │         ├──< InstanceData
    │          │         └──< InstanceRoute >── Route
    │          ├──< FlowCompiledArtifact
    │          └──< FlowPublishedFieldMap
    ├──< FlowField
    └──< FlowTranslation

Instance ──< Instance (self-ref parent/children)
```

---

---

# BiApp.Authentication — Entity Referansı

## Entityler (6)
> Tüm entity'ler `BaseEntity` kullanır. Navigation property yoktur; dış servis referansları yalnızca Guid FK olarak tutulur.

---

### User
**Tablo:** `USER` · **Base:** `BaseEntity`

| Alan | Tip | Zorunlu |
|------|-----|---------|
| FullName | string | ✓ |
| Identity | string | ✓ |
| Password | string | ✓ (encrypted) |

---

### ApplicationUser
**Tablo:** `APPLICATION_USER` · **Base:** `BaseEntity`

| Alan | Tip | Zorunlu |
|------|-----|---------|
| ApplicationId | Guid | ✓ |
| UserId | Guid | ✓ |

---

### Login
**Tablo:** `LOGIN` · **Base:** `BaseEntity`

| Alan | Tip | Zorunlu |
|------|-----|---------|
| ApplicationId | Guid | ✓ |
| Token | string | ✓ |
| CustomerId | Guid | ✓ |
| Identity | string | ✓ |
| ShowOnlyResource | bool | ✓ |
| Logout | bool | ✓ |

---

### LoginSession
**Tablo:** `LOGIN_SESSION` · **Base:** `BaseEntity`

| Alan | Tip | Zorunlu |
|------|-----|---------|
| ApplicationId | Guid | ✓ |
| CustomerId | Guid | ✓ |
| Identity | string | ✓ |
| ExpireDate | DateTime | ✓ |
| Token | string | ✓ (encrypted) |

---

### ClientLogin
**Tablo:** `CLIENT_LOGIN` · **Base:** `BaseEntity`

| Alan | Tip | Zorunlu |
|------|-----|---------|
| ClientId | Guid | ✓ |
| ApplicationId | Guid | ✓ |
| IpAddress | string? | - |
| LoginTime | DateTime | ✓ |

---

### UserDevice
**Tablo:** `USER_DEVICE` · **Base:** `BaseEntity`

| Alan | Tip | Zorunlu |
|------|-----|---------|
| ApplicationId | Guid | ✓ |
| CustomerId | Guid | ✓ |
| DeviceId | string | ✓ |
| PushToken | string | ✓ |
| PublicKey | string | ✓ |
| DeviceModel | string? | - |
| Platform | string? | - |
| OsVersion | string? | - |
| AppVersion | string? | - |

---

## Notlar (Authentication)

- **Şifreli alanlar:** `User.Password`, `LoginSession.Token` — DB'de şifreli tutulur.
- **Bounded context:** Dış entity'lere (ApplicationId, CustomerId, UserId, ClientId) navigation property olmadan yalnızca Guid FK ile referans verilir.

---

---

# BiApp.Parameters — Entity Referansı

## Entityler (12)

### Parameter
**Tablo:** `PARAMETER` · **Base:** `TenantBaseEntity`

| Alan | Tip | Zorunlu |
|------|-----|---------|
| MicroserviceId | Guid | ✓ |
| Name | string | ✓ |
| Type | Guid | ✓ |
| ServiceId | Guid? | - |
| ServiceIdForNames | Guid? | - |
| ColumnCount | int | ✓ |

**İlişkiler:** ParameterValues (1:N), ParameterServiceMaps (1:N), ParameterTranslations (1:N)

---

### ParameterTranslation
**Base:** `TranslationBaseEntity`  
**İlişkiler:** Parameter (FK via RecordId)

---

### ParameterValue
**Tablo:** `PARAMETER_VALUE` · **Base:** `BaseEntity`

| Alan | Tip | Zorunlu |
|------|-----|---------|
| ParameterId | Guid | ✓ |
| Name | string | ✓ |
| Name2 | string? | - |
| Name3 | string? | - |
| Name4 | string? | - |
| Name5 | string? | - |
| Order | int | ✓ |

**İlişkiler:** Parameter (FK), ParameterValueTranslations (1:N)

---

### ParameterValueTranslation
**Base:** `TranslationBaseEntity`  
**İlişkiler:** ParameterValue (FK via RecordId)

---

### ParameterServiceMap
**Tablo:** `PARAMETER_SERVICE_MAP` · **Base:** `BaseEntity`

| Alan | Tip | Zorunlu |
|------|-----|---------|
| ParameterId | Guid | ✓ |
| ValueProperty | string | ✓ |
| NameProperty | string | ✓ |
| Name2Property | string? | - |
| Name3Property | string? | - |
| Name4Property | string? | - |
| Name5Property | string? | - |
| OrderProperty | string? | - |

**İlişkiler:** Parameter (FK)

---

### Country
**Tablo:** `COUNTRY` · **Base:** `BaseEntity`

| Alan | Tip | Zorunlu |
|------|-----|---------|
| LanguageId | Guid | ✓ |
| CurrencyId | Guid | ✓ |
| Code | string | ✓ |
| Name | string | ✓ |
| LocalName | string | ✓ |
| CallingCode | string? | - |

**İlişkiler:** CountryTranslations (1:N)

---

### CountryTranslation
**Base:** `TranslationBaseEntity`  
**İlişkiler:** Country (FK via RecordId)

---

### State
**Tablo:** `STATE` · **Base:** `BaseEntity`

| Alan | Tip | Zorunlu |
|------|-----|---------|
| CountryId | Guid | ✓ |
| Code | string | ✓ |
| Name | string | ✓ |

**İlişkiler:** Country (FK), StateTranslations (1:N)

---

### StateTranslation
**Base:** `TranslationBaseEntity`  
**İlişkiler:** State (FK via RecordId)

---

### City
**Tablo:** `CITY` · **Base:** `BaseEntity`

| Alan | Tip | Zorunlu |
|------|-----|---------|
| StateId | Guid | ✓ |
| Code | string | ✓ |
| Name | string | ✓ |

**İlişkiler:** State (FK), CityTranslations (1:N)

---

### CityTranslation
**Base:** `TranslationBaseEntity`  
**İlişkiler:** City (FK via RecordId)

---

### Calendar
**Tablo:** `CALENDAR` · **Base:** `BaseEntity`

| Alan | Tip | Zorunlu |
|------|-----|---------|
| CountryId | Guid | ✓ |
| Date | DateOnly | ✓ |
| Type | Guid | ✓ |
| HalfDay | bool | ✓ |
| StartHour | TimeOnly | ✓ |
| EndHour | TimeOnly | ✓ |
| Description | string? | - |

**İlişkiler:** Country (FK)

---

## İlişki Haritası (Parameters)

```
Parameter ──< ParameterValue ──< ParameterValueTranslation
         ──< ParameterServiceMap
         ──< ParameterTranslation

Country ──< State ──< City ──< CityTranslation
       ──< CountryTranslation    └── StateTranslation
       ──< Calendar
```
