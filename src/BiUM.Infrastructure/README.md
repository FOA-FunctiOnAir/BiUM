# BiUM.Infrastructure

## 📖 Overview
**BiUM.Infrastructure** provides the concrete implementations for services defined in `BiUM.Core`. It handles external dependencies like databases, message brokers, and caching systems.

## 🔑 Key Implementations

- **Persistence**: Entity Framework Core base repositories and DbContexts.
- **Message Broker**: RabbitMQ implementation for `IEventBus` using `MassTransit` or native clients.
- **Caching**: Redis implementation for `ICacheService` (`StackExchange.Redis`).
- **Logging**: OpenTelemetry configuration for Metrics, Tracing, and Logging.
- **File Services**: HTML to PDF conversion using `SimpleHtmlToPdf`.
- **Health Checks**: Standardized health checks for dependencies.

## 📦 Usage

This layer is typically referenced by `BiUM.Specialized` or the application startup project directly.

### Service Registration

```csharp
// Configures EF Core, RabbitMQ, Redis, and OpenTelemetry
services.AddInfrastructureServices(hostBuilder, configuration);
```

### Configuration
Ensure your `appsettings.json` has the required sections:

```json
{
  "RabbitMQ": { ... },
  "Redis": { ... },
  "ConnectionStrings": { ... }
}
```
