# Agents.DomainModelConventions.md — FOA entity, column, DTO, and translation naming

This document defines **cross-service naming and modeling rules** for **BiApp.\*** microservices built on BiUM (`BaseEntity`, EF Core, translation tables). Apply these rules consistently in **Domain entities**, **EF configurations**, **Application DTOs**, **commands/queries**, and **API contracts** when adding or refactoring persistence models.

**Reference implementation:** BiApp.Bpmn **Flow** and its flow translation table (repositories, commands, queries). BiApp.Subscription **FeatureDefinition** / **PricingPlan** translations follow the same pattern.

Stable URL: [Agents.DomainModelConventions.md](https://github.com/FOA-FunctiOnAir/BiUM/blob/master/Agents.DomainModelConventions.md)

---

## 1. Active flag — use `BaseEntity` only

Entities that inherit **`BaseEntity`** already expose **`Active`** (`bool`), mapped to the shared **`ACTIVE`** column pattern used across FOA.

- **Do not** add a second flag such as `IsActive`, or a duplicate column mapped with `[Column("IS_ACTIVE")]` (or equivalent).
- Use **`Active`** in code and filter with **`.Where(e => e.Active)`** where catalog or business rules require “active only” rows.
- Initialisation / seeding should set **`Active`** via the same path as other `BaseEntity` fields (e.g. shared stamp helpers), not a parallel property.

---

## 2. Boolean fields — no `IS_` column prefix; C# name without leading `Is`

For boolean semantics that are **not** the shared `BaseEntity.Active`:

- **C#:** Prefer **`Highlighted`**, **`Enabled`**, **`Required`**, etc. — **not** `IsHighlighted`, `IsEnabled`, … unless the domain term is already fixed elsewhere (still avoid duplicating `Active`).
- **Database column:** Map to **`HIGHLIGHTED`**, **`ENABLED`**, **`REQUIRED`**, etc. — **not** `IS_HIGHLIGHTED`, `IS_ENABLED`, …
- **DTOs, commands, queries, and API shapes** must use the **same** names as the entity for the same concept (avoid `IsEnabled` on the API when the entity uses `Enabled`).

`BaseEntity.Active` remains the single “row active / soft-business-active” flag; do not re-express it as another boolean with an `IS_` column.

---

## 3. Date and time properties — no `Utc` suffix on names

Store and expose **semantic** timestamps; whether values are stored in UTC is a **convention at write time**, not part of the **property name**.

- **Do not** use suffixes such as **`Utc`** on property names (`StartedUtc`, `CreatedUtc`, `ConfirmedAtUtc`, `UsedAtUtc`, …).
- **Do** use names like **`Started`**, **`Ended`**, **`Created`** (when meaning full `DateTime`; see also `BaseEntity` **`Created`** as `DateOnly` + **`CreatedTime`**), **`ConfirmedAt`**, **`PaidAt`**, **`CancelledAt`**, **`UsedAt`**, **`ValidFrom`**, **`ValidTo`**, etc.
- Align **EF column** names with the same rule (e.g. `STARTED`, `CONFIRMED_AT` — avoid `*_UTC` in column names unless an existing legacy table forces otherwise).

Avoid naming clashes: if a DTO needs a full `DateTime` “created” while the entity uses `DateOnly` + `TimeOnly`, prefer a distinct but clear name (e.g. document the DTO field as the combined audit instant) without resorting to `Utc` in the name.

---

## 4. Localisable strings — side table + `Column`, not `TranslationCode` on the parent

When an entity field (e.g. **`Name`**, **`Description`**) should be translated:

1. Keep the **main table** columns for the **default** (or authoring) values as today (`Name`, etc.).
2. Add a **translation table** (e.g. `{Entity}Translation`) inheriting your stack’s **`TranslationBaseEntity`** (or equivalent): **`RecordId`**, **`LanguageId`**, **`Column`**, **`Translation`**, …
3. **Rows** in the translation table identify the field with **`Column`** set to the **C# property name** (e.g. `"Name"`, `"Description"`). Multiple translatable columns on the same entity use **multiple rows** (same `RecordId`, different `Column` values) — not extra linkage codes on the parent.
4. **Do not** add **`TranslationCode`** (or similar lookup keys) on the **parent** entity for this mechanism. **Do not** add a **`Translations`** navigation collection on the parent **solely** for EF configuration if your pattern configures the relationship from the translation entity only (see below).
5. **EF Core:** Configure **`HasOne(translation => translation.Parent).WithMany()`** — **no** inverse collection on the parent, matching the Flow translation pattern in BiApp.Bpmn.

Queries and commands that **read or write** translations should follow the same **Column = property name** rule so tooling and seed data stay aligned.

---

## 5. Error code naming — `AddMessage()` codes

Every string passed to **`AddMessage(response, code, cancellationToken)`** is an **error code** that is looked up at runtime in `dbo.__TRANSLATION` (keyed by `CODE + APPLICATION_ID`). If no matching row exists the client receives `translation_not_found`.

### 5.1 Format rule — always `snake_case`

Codes **must** be `snake_case`. PascalCase (`"ProfessionNotFound"`) and human-readable strings (`"Profession Not Found"`) are **never** inserted into `__TRANSLATION`, so they will always fail lookup at runtime.

| Wrong | Correct |
|-------|---------|
| `"ProfessionNotFound"` | `"profession_not_found"` |
| `"RoleNotFound"` | `"role_not_found"` |
| `"Tenant Registration Not Found"` | `"tenant_registration_not_found"` |
| `"profession not found"` | `"profession_not_found"` |

### 5.2 Standard code patterns

Use these canonical forms. Do not invent synonyms — the pattern must match the DB row exactly.

| Situation | Pattern | Example |
|-----------|---------|---------|
| Required field missing | `X_required` | `"id_required"`, `"application_required"` |
| Record not found | `X_not_found` | `"customer_not_found"`, `"role_not_found"` |
| Duplicate / already exists | `X_already_exists` | `"customer_is_already_exists_with_same_identity"` |
| Conflict / already defined | `X_already_defined` | `"portfolio_is_already_defined_this_customer"` |
| Generic operation failure | `X_failed` | `"save_failed"` |

**`X_not_found` is the canonical form.** The alias `no_X_found` is a semantic duplicate — always use `X_not_found`.

### 5.3 Typo audit before committing

Before adding a new code, search the file for existing variants:

- Misspellings in the middle (`"registrration"`, `"porfolio"`, `"aleady"`)
- `_exist_` vs `_exists_` — prefer `_exists_`

A typo in a code string is a silent runtime failure; the lookup will never match even if the DB row is correct.

### 5.4 DB insertion requirement

For every new code used in code there **must** be a corresponding row in `__TRANSLATION` + `__TRANSLATION_DETAIL` for all active languages. Use `APPLICATION_ID = Ids.Application.BiDynamic.Id` (`5abc0777-19ff-5993-b71e-38cc9f7673a2`) so codes are shared across microservices via the BiDynamic fallback.

Fallback resolution order at runtime (see `TranslationService.GetTranslation`):

1. Lookup by `correlationContext.ApplicationId` + `code`
2. If not found **and** current app ≠ BiDynamic → lookup by `Ids.Application.BiDynamic.Id` + `code`
3. Returns `null` → response carries `translation_not_found`

---

## 6. Entity lifecycle events — every entity defines Created / Updated / Deleted

Every entity in a **BiApp.\*** service defines its three lifecycle events and returns them from `BaseEntity`'s virtual hooks. `EntitySaveChangesInterceptor` calls `AddCreatedEvent` / `AddUpdatedEvent` / `AddDeletedEvent` through `IBaseEntity` on every `SaveChanges` (soft delete counts as delete) and publishes the result after commit.

**Event classes** (`<Service>.Domain/Events/<Entity>CreatedEvent.cs`, `…UpdatedEvent.cs`, `…DeletedEvent.cs`, one type per file):

```csharp
[Event]
[MemoryPackable]
public partial class ApplicationCreatedEvent : TenantBaseEntityEvent, IMapFrom<Application>
{
}
```

- Base: `TenantBaseEntityEvent` for `TenantBaseEntity`, `BaseEntityEvent` for `BaseEntity`. `Id` carries the entity id (mapped from the entity).
- `[Event]` with an empty `Exchange` (broadcast from the owning service), `[MemoryPackable]` and `partial` are all required; RabbitMQ messages are MemoryPack-serialised (see [Agents.MessageBroker.md](Agents.MessageBroker.md)).
- Add properties only for fields consumers need; an empty body is valid.
- Scope: entities deriving from `BaseEntity` / `TenantBaseEntity`. Translation rows (`TranslationBaseEntity`) are not `IBaseEntity` and get no lifecycle events.
- The fanout exchange name comes from the event type name (`{domain}.{entity}_{kind}_event`). If the service already publishes a hand-written event with the same name (other consumers depend on its schema), do not add the generic one for that kind; keep the existing event (e.g. BiApp.Configuration `AccessPointDeletedEvent`, consumed by Bpmn).

**Entity hooks** — `override`, never `new` (a `new` method hides the hook and is never called through `IBaseEntity`, so no event is produced):

```csharp
public override IBaseEvent? AddCreatedEvent(IMapper? mapper, IBaseEvent? baseEvent)
{
    var createdEvent = mapper?.Map<ApplicationCreatedEvent>(this);

    return base.AddCreatedEvent(mapper, createdEvent);
}
```

Same shape for `AddUpdatedEvent` (`…UpdatedEvent`) and `AddDeletedEvent` (`…DeletedEvent`). Reference implementation: BiApp.Configuration `Configuration.Domain/Entities/Application.cs` + `Configuration.Domain/Events/Application*Event.cs`.

**Legacy to fix when touched:** Generator-era `*.g.cs` entity files with `public new IBaseEvent? Add…Event(…) => CreateEvent<…>(mapper)` and event classes without `[MemoryPackable] partial` produce no events. When editing such a service, convert them to the pattern above (and merge/remove the `*.g.cs` files — no `*.g.cs` in services).

---

## 7. Parameter value columns — `_TYPE` / `…Type` suffix, not `_ID`

A column that stores a **parameter value** (a `ParameterValue` id from BiApp.Parameters, e.g. status, difficulty, category, direction) is named with the **`Type`** suffix:

| Meaning | Column | C# property |
|---------|--------|-------------|
| Status parameter | `STATUS_TYPE` | `StatusType` |
| Difficulty parameter | `DIFFICULTY_TYPE` | `DifficultyType` |
| Category parameter | `CATEGORY_TYPE` | `CategoryType` |

- **`_ID` / `…Id`** is used only for references to **rows of another table** (`LESSON_ID`, `TOPIC_ID`, `COUNTRY_ID`, `LANGUAGE_ID`, …).
- Apply the same name to DTOs, commands and query filters (`Guid? StatusType`).
- Existing examples: BiApp.Configuration `ServiceParameter.DirectionType`, `Service.HttpType`, `Service.Type`.
- Legacy `…Id` parameter columns are renamed when the service is touched (new services: migration; old services without migrations: SQL script).

---

## 8. Agent workflow

When editing entities or DTOs in any **BiApp.\*** service:

1. Re-read **[Agents.MsStructure.md](Agents.MsStructure.md)** for layer placement.
2. Apply **this document** for naming and translation shape.
3. If behaviour touches BiUM pipelines (transactions, CRUD publishing, etc.), use the relevant **Agents.\*.md** from the BiUM `AGENTS.md` index.

When you introduce or change a **team-wide** rule that contradicts or extends this file, update **this file** and, if needed, **[Agents.md](Agents.md)** links so agents and developers stay in sync.
