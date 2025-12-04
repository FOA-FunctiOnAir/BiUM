# BiUM.Bolt

## Overview
`BiUM.Bolt` is a library designed to facilitate the configuration and initialization of database connections, specifically targeting PostgreSQL. It provides extensions for `IServiceCollection` to easily register database contexts and initializers.

## Key Features
-   **PostgreSQL Support**: Configures PostgreSQL connections with retry policies and connection pooling.
-   **Dynamic Connection Strings**: Supports dynamic connection string generation based on configuration options (`BoltOptions`).
-   **Database Initialization**: Includes interfaces and classes for database initialization (`IBaseBoltDbContextInitialiser`, `BoltDbContextInitialiser`).
-   **Interceptors**: Registers `BoltEntitySaveChangesInterceptor` for handling entity changes.

## Usage
To use `BiUM.Bolt`, call the `AddBolt` extension method in your startup configuration:

```csharp
services.AddBolt<MyDbContext, MyDbContextInitialiser>(configuration);
```

Ensure your configuration includes the necessary `BoltOptions` and connection strings.
