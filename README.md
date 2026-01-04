> [!IMPORTANT]
> **For AI Agents**: Please refer to [AGENTS.md](AGENTS.md) for context, conventions, and operational guidelines before proceeding with any tasks.

# BiUM (BiApp Universal Modules)

**BiUM** is the foundational shared library for the **FunctiOnAir (FOA)** microservices ecosystem. It provides core building blocks, abstractions, and infrastructure implementations used across all business applications to ensure consistency in logging, data access, messaging, and error handling.

## 🚀 Getting Started

To use BiUM in your project, install the `BiUM.Specialized` package. This package aggregates core functionalities and common configurations.

```bash
dotnet add package BiUM.Specialized --source "https://nuget.pkg.github.com/FOA-FunctiOnAir/index.json"
```

## 🛠️ Build and Test

1. **Restore dependencies**:
   ```bash
   dotnet restore
   ```
2. **Build the solution**:
   ```bash
   dotnet build
   ```
3. **Run tests**:
   ```bash
   dotnet test
   ```

## 📦 Modules

BiUM is modular, allowing you to consume only what you need:

- **[BiUM.Core](src/BiUM.Core/README.md)**: The heart of the library. Contains interfaces, base classes, core utilities, and abstractions for Caching, Database, Logging, and Messaging.
- **[BiUM.Infrastructure](src/BiUM.Infrastructure/README.md)**: Concrete implementations of Core abstractions, including EF Core, Redis, RabbitMQ, and OpenTelemetry integrations.
- **[BiUM.Bolt](src/BiUM.Bolt/README.md)**: A lightweight, specialized data access module for high-performance scenarios, enabling easier database configuration.
- **[BiUM.Specialized](src/BiUM.Specialized/README.md)**: High-level components for application setup, including API configurations, Interceptors, Mapping, and domain-agnostic services.
- **[BiUM.Contract](src/BiUM.Contract/README.md)**: Shared Data Transfer Objects (DTOs) and gRPC contracts for inter-service communication.

## 📦 NuGet Package Installation Guide

To consume BiUM packages from the GitHub Package Registry, you must configure your NuGet client with authentication.

### 1. Generate a Personal Access Token (PAT)
1. Go to **GitHub Settings** > **Developer settings** > **Personal access tokens**.
2. Generate a new token (Classic).
3. Select the `read:packages` scope.
4. Copy the token.

### 2. Configure `nuget.config`
Add a `nuget.config` file to your solution root:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="bium" value="https://nuget.pkg.github.com/foa-functionair/index.json" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
  <packageSourceCredentials>
    <bium>
      <add key="Username" value="GITHUB_USERNAME" />
      <add key="ClearTextPassword" value="GITHUB_TOKEN" />
    </bium>
  </packageSourceCredentials>
</configuration>
```
*Replace `GITHUB_USERNAME` with your GitHub username and `GITHUB_TOKEN` with your PAT.*

> [!NOTE]
> For CI/CD (GitHub Actions), use the automatically provided `GITHUB_TOKEN` instead of a PAT.

## 📚 Project Documentation

Detailed documentation for each module is available in their respective directories:

- [BiUM.Bolt Documentation](src/BiUM.Bolt/README.md)
- [BiUM.Contract Documentation](src/BiUM.Contract/README.md)
- [BiUM.Core Documentation](src/BiUM.Core/README.md)
- [BiUM.Infrastructure Documentation](src/BiUM.Infrastructure/README.md)
- [BiUM.Specialized Documentation](src/BiUM.Specialized/README.md)
