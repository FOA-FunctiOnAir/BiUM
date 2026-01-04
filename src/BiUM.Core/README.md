# BiUM.Core

## 📖 Overview
**BiUM.Core** is the foundational library for the BiUM solution, containing common utilities, interfaces, and shared services used across the application to ensure consistency and modularity.

## 🔑 Key Components

- **Common**:
  - `Configs`: Options pattern classes (`BoltOptions`, `RabbitMQOptions`).
  - `Utils`: Helper methods (`DateTime`, `String` extensions).
- **Caching**: `ICacheService` interfaces for distributed and memory caching.
- **Database**:
  - `IRepository<T>`: Generic repository interface.
  - `IUnitOfWork`: Transaction management abstraction.
- **MessageBroker**: `IEventBus` interface for publishing integration events.
- **Logging**: Abstractions for structured logging.
- **HttpClients**: Base classes for typed HTTP clients.

## 📦 Usage

Add the project reference or install the NuGet package:

```bash
dotnet add package BiUM.Core
```

### Service Registration
Register core services in your startup configuration:

```csharp
// Registers MediatR, AutoMapper validators, and core services
services.AddCoreServices(assembly);
services.AddFileServices(assembly);
```
