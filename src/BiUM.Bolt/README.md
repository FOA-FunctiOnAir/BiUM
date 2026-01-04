# BiUM.Bolt

## 📖 Overview
**BiUM.Bolt** is a specialized module for configuring **PostgreSQL** data access. It simplifies `DbContext` registration and initialization, building upon `BiUM.Specialized`.

## 🔑 Key Features
- **PostgreSQL Focus**: Pre-configured connection pooling and retry policies for Npgsql.
- **Dynamic Configuration**: `BoltOptions` allows connection strings to be resolved dynamically (e.g., via config or secrets).
- **Initialization**: Provides `BoltDbContextInitialiser` for automated migrations and seeding.
- **Interceptors**: Automatically registers auditing interceptors.

## 📦 Usage

Add the `AddBolt` extension in your startup:

```csharp
// Registers DbContext with PostgreSQL and related services
services.AddBolt<MyDbContext, MyDbContextInitialiser>(configuration);
```

### Configuration (`appsettings.json`)

```json
"Bolt": {
  "Enable": true,
  "ConnectionString": "Host=localhost;Database=mydb;Username=postgres;Password=password"
}
```
