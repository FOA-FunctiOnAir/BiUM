# BiUM.Contract

## Overview
`BiUM.Contract` is a shared library that defines common gRPC messages and contracts used across the BiUM solution. It ensures consistency in gRPC communication structures.

## Key Components
-   **common.proto**: Defines standard gRPC messages such as:
    -   `GrpcRequestMeta`: Metadata for requests.
    -   `GrpcResponseMeta`: Standard response metadata including success status and messages.
    -   `GrpcResponseMessage`: Structure for messages (code, message, exception, severity).
    -   `GrpcIdNameMessage`: A simple ID-Name pair message.

## Usage
This project is referenced by other services that need to implement or consume these standard gRPC contracts.
