# BiUM.Core

## Overview
`BiUM.Core` is the foundational library for the BiUM solution, containing common utilities, interfaces, and shared services used across the application.

## Key Components
-   **Common**: Shared utilities and helper classes.
-   **Caching**: Interfaces and implementations for caching mechanisms.
-   **Database**: Core database abstractions.
-   **HttpClients**: HTTP client configurations and services.
-   **Logging**: Logging abstractions and configurations.
-   **MessageBroker**: Interfaces for message brokering.
-   **Models**: Shared data models.
-   **File Services**: Integration with `SimpleHtmlToPdf` for HTML to PDF conversion.

## Usage
This project is referenced by almost all other projects in the solution to ensure access to core functionalities and standardizations.

### Service Registration
To use the core services, register them in your startup configuration:

```csharp
services.AddCoreServices(assembly);
services.AddFileServices(assembly);
```
