# BiUM.Contract

## 📖 Overview
**BiUM.Contract** defines shared data structures, DTOs, and gRPC service contracts used for communication between microservices.

## 🔑 Key Components

- **common.proto**: Standard gRPC message definitions used across the ecosystem.
  - `GrpcRequestMeta`: Standard request metadata (trace IDs, user context).
  - `GrpcResponseMeta`: Standard response metadata (success status, error codes).
  - `GrpcIdNameMessage`: Reusable ID/Name pair message.

## 📦 Usage

Add this package to any service needing to consume or implement shared contracts.

```bash
dotnet add package BiUM.Contract
```

The `.proto` files are packed into the NuGet package and can be imported into other proto files.
