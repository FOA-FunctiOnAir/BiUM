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

## 5. Agent workflow

When editing entities or DTOs in any **BiApp.\*** service:

1. Re-read **[Agents.MsStructure.md](Agents.MsStructure.md)** for layer placement.
2. Apply **this document** for naming and translation shape.
3. If behaviour touches BiUM pipelines (transactions, CRUD publishing, etc.), use the relevant **Agents.\*.md** from the BiUM `AGENTS.md` index.

When you introduce or change a **team-wide** rule that contradicts or extends this file, update **this file** and, if needed, **[AGENTS.md](AGENTS.md)** links so agents and developers stay in sync.
