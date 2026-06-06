# Agents.Coding.md — FOA C# coding style

Shared **format and layout rules** for all `BiApp.*` microservices (and BiUM library code). Enforced locally via `format-and-build.ps1` and `.editorconfig` at each solution root. **AI-generated code must comply before commit.**

## 1. Verification

From any microservice repository root:

```powershell
.\format-and-build.ps1
```

The script runs, in order:

1. `dotnet format whitespace` — whitespace and indentation (`.editorconfig`)
2. `dotnet format style --diagnostics IDE0005` — remove unused `using` directives
3. **EOF cleanup** — strips trailing blank lines from `*.cs`, `*.csproj`, `*.json`
4. `dotnet build` — must succeed

Generated or edited code should pass this script without manual fixes.

## 2. File and general format

| Rule | Source |
|------|--------|
| UTF-8 encoding | `.editorconfig` |
| Line endings: **CRLF** | `.editorconfig` |
| C# indent: **4 spaces** | `.editorconfig` |
| JSON / csproj indent: **2 spaces** | `.editorconfig` |
| Trim trailing whitespace on lines | `.editorconfig` |
| **No extra blank line at end of file** | `.editorconfig` (`insert_final_newline = false`) + `format-and-build.ps1` EOF step |
| **No consecutive blank lines** (at most one blank line between blocks) | `.editorconfig` |
| No blank line between consecutive closing braces `}` | `.editorconfig` |

## 3. C# structure (.editorconfig)

| Rule | Example |
|------|---------|
| **File-scoped namespace** (required) | `namespace BiApp.X.Application.Features.Y;` |
| Opening brace on new line | `if (condition)` then `{` on next line |
| Braces required for all control blocks | Even single-statement `if` uses `{ }` |
| `using` at top of file, outside namespace | Sorted; unused usings removed (IDE0005) |
| Prefer `var` for locals when type is apparent | `var response = new ApiResponse<T>();` |
| Accessibility modifiers always written | `public`, `private`, etc. |
| Expression-bodied members | Properties, indexers, accessors, lambdas: yes; methods, constructors: no |

## 4. Naming

| Element | Convention |
|---------|------------|
| Interface | `I` prefix — `ICustomersClient` |
| Type, method, property, event | PascalCase |
| Private instance field | `_camelCase` |
| Private static field | PascalCase |
| `const` field | PascalCase |

## 5. Comments and dead code

- No inline or XML documentation comments; code should be self-explanatory.
- Remove unused methods, files, and `using` directives.
- PowerShell scripts (`format-and-build.ps1`, etc.) contain no comment blocks.

## 6. Blank-line layout (team convention)

These rules are **not** fully automated by `dotnet format`; agents and reviewers apply them when writing code.

### 6.1 Same-kind statements — no blank line between

Statements of the same kind in the same logical step may sit **back-to-back** without a blank line:

```csharp
var response = new ApiResponse<MeDto>();
var me = meResponse.Value;
```

```csharp
dto.Id = me.Id;
dto.Fn = me.Fn;
dto.N = me.N;
```

### 6.2 Control flow and exits — blank line when preceded by code

When an **`if`**, **`else if`**, **`return`**, **`throw`**, **`foreach`**, **`switch`**, or **`try`** is not the first statement in its block, insert **one blank line** before it if the line immediately above is another statement (not `{`, not another `else`).

Applies to:

- `if` / `else if` (not `else` alone — see §6.3)
- `return`
- `throw`
- `foreach`
- `switch`
- `try`

**Wrong** — missing blank line before `if`:

```csharp
var meResponse = await _customersClient.GetFwMe(cancellationToken);
if (!meResponse.Success || meResponse.Value is null)
{
    response.AddMessage(meResponse.Messages);
    return response;
}
```

**Correct**:

```csharp
var meResponse = await _customersClient.GetFwMe(cancellationToken);

if (!meResponse.Success || meResponse.Value is null)
{
    response.AddMessage(meResponse.Messages);

    return response;
}
```

**Wrong** — missing blank line before `return`:

```csharp
response.Value = dto;
return response;
```

**Correct**:

```csharp
response.Value = dto;

return response;
```

Inside a block, when several statements run before an exit:

```csharp
await AddMessage(response, "call_service_error", cancellationToken);

return response;
```

### 6.3 `else` — no blank line before

`else` and `else if` stay directly after the closing `}` of the preceding branch — **no** blank line before `else`:

```csharp
if (responseApi == null)
{
    await AddMessage(response, "call_service_error", cancellationToken);

    return response;
}
else if (!responseApi.Success)
{
    response.AddMessage(responseApi.Messages);

    return response;
}
```

### 6.4 No blank line at block entry

Do not add a blank line as the first line inside a method or inside `{ }` after `if`, `foreach`, etc.:

```csharp
public async Task<ApiResponse<MeDto>> Handle(...)
{
    var response = new ApiResponse<MeDto>();
```

### 6.5 Logical sections

Separate distinct steps (prepare → external call → branch → assign result → return) with **a single** blank line.

## 7. Reference handler (target style)

```csharp
public async Task<ApiResponse<MeDto>> Handle(GetFwMeQuery query, CancellationToken cancellationToken)
{
    var response = new ApiResponse<MeDto>();

    var meResponse = await _customersClient.GetFwMe(cancellationToken);

    if (!meResponse.Success || meResponse.Value is null)
    {
        response.AddMessage(meResponse.Messages);

        return response;
    }

    var me = meResponse.Value;

    var dto = new MeDto
    {
        Id = me.Id,
        Fn = me.Fn
    };

    var themeResponse = await _configurationClient.GetMyApplicationTheme(query.ThemeOverrideVersion, cancellationToken);

    if (themeResponse.Success)
    {
        dto.Tho = themeResponse.Value;
    }

    response.Value = dto;

    return response;
}
```

## 8. Related docs

- [Agents.MsStructure.md](Agents.MsStructure.md) — solution layout and BiUM pipeline
- [Agents.DomainModelConventions.md](Agents.DomainModelConventions.md) — entity and DTO naming
- Per-service `AGENTS.md` — business domain
