# BiUM.Infrastructure

## Overview
`BiUM.Infrastructure` provides the concrete implementations and configuration for infrastructure services defined in `BiUM.Core` and other layers. It handles the setup of external dependencies and cross-cutting concerns.

## Key Services
-   **gRPC**: Configures gRPC services and reflection.
-   **Redis**: Implements `IRedisClient` and configures Redis connection options.
-   **RabbitMQ**: Implements `IRabbitMQClient`, configures listeners, and registers event handlers.
-   **Serilog**: Configures logging with Serilog, including console and file sinks (configurable via `Specialized` options).
-   **Health Checks**: Adds health check services.

## Configuration
The `AddInfrastructureServices` extension method accepts a `Specialized` configuration object or binds it from `IConfiguration`. This allows for flexible configuration of infrastructure components.

## Usage
To register infrastructure services:

```csharp
services.AddInfrastructureServices(host, configuration);
```
