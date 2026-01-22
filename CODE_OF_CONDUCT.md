# Technical Code of Conduct - BiUM (Core Library)

## 1. Purpose
This document defines the **Technical Standards and Rules of Engagement** for contributing to **BiUM**. As the foundational library for the entire FOA ecosystem, code in this repository must adhere to the highest standards of quality, stability, and reusability.

## 2. Architectural Principles

### 2.1. Domain Agnostic
- **Rule**: Code in `BiUM` MUST NOT contain specific business logic related to any single domain (e.g., "Invoice calculation", "Customer validation").
- **Goal**: Provide generic building blocks (Repositories, Event Bus, Logging) that any service can use.

### 2.2. Stability & Compatibility
- **Rule**: Avoid breaking changes. If a signature change is necessary, use `[Obsolete]` attributes and provide a migration path.
- **Goal**: Prevent breaking the 20+ services that depend on this library.

### 2.3. Abstraction First
- **Rule**: Expose functionality via Interfaces (`IUserLookupService`) rather than concrete classes.
- **Goal**: Enable dependency injection and testability in consuming services.

## 3. Coding Standards

### 3.1. C# Guidelines
- **Framework**: Target **.NET 10**.
- **Nullable Reference Types**: Enabled (`<Nullable>enable</Nullable>`).
- **Async/Await**: Use `async` for all I/O bound operations. Always pass `CancellationToken`.
- **Naming**:
    - Interfaces: `I` prefix (e.g., `IRepository`).
    - Async Methods: `Async` suffix (e.g., `GetByIdAsync`).
    - Private Fields: `_camelCase`.

### 3.2. Project Structure
- **`BiUM.Core`**: Interfaces, Exceptions, Base DTOs. No heavy dependencies.
- **`BiUM.Infrastructure`**: Concrete implementations (EF Core, MassTransit).
- **`BiUM.Bolt`**: Lightweight, high-performance implementations.

## 4. Testing Standards

### 4.1. Unit Testing
- **Requirement**: **100% Branch Coverage** for `BiUM.Core` and logical components in `BiUM.Infrastructure`.
- **Tools**: xUnit, Moq, FluentAssertions.
- **Rule**: Do not mock `DbContext` directly; use the In-Memory provider or a repository abstraction.

## 5. Dependency Management
- **Nuget**: All external dependencies must be approved. Avoid bringing in heavy libraries for simple utility functions.
- **Versioning**: Follow Semantic Versioning (SemVer).

## 6. AI Agent Guidelines
- When refactoring, **always** run all tests in `BiUM` and at least one consuming service (if possible) to ensure no regressions.
- If you add a new utility, add a corresponding Unit Test.
