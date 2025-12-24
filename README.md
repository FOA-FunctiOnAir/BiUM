> [!IMPORTANT]
> **For AI Agents**: Please refer to [AGENTS.md](AGENTS.md) for context, conventions, and operational guidelines before proceeding with any tasks.

# Introduction

**BiUM** is the foundational shared library for the FunctiOnAir (FOA) microservices ecosystem. It provides core building blocks, abstractions, and infrastructure implementations used across all business applications to ensure consistency in logging, data access, messaging, and error handling.

# Getting Started

To use BiUM in your project, install the `BiUM.Specialized` package:

```bash
dotnet add package BiUM.Specialized --source "https://nuget.pkg.github.com/FOA-FunctiOnAir/index.json"
```

# Build and Test

1. Restore dependencies:
   ```bash
   dotnet restore
   ```
2. Build the solution:
   ```bash
   dotnet build
   ```
3. Run tests:
   ```bash
   dotnet test
   ```

# Modules

- **BiUM.Core**: Interfaces and base classes.
- **BiUM.Infrastructure**: Concrete implementations (EF Core, Services).
- **BiUM.Bolt**: Lightweight data access.
- **BiUM.Specialized**: Advanced components (Interceptors, Mapping).
- **BiUM.Contract**: Shared DTOs.

<!-- Push steps
1 - Run this command line to remove all credentials : dotnet nuget locals all --clear
2 - Go to Solution .sln|.slnx path where nuget.config file is available run : dotnet restore --interactive
3 - Add these attributes to the project that you want to pack :
  --<GeneratePackageOnBuild>true</GeneratePackageOnBuild>
  --<PackageId>EasyAccess</PackageId>
  --<Version>1.0.1</Version>
  --<Authors>BiApp</Authors>
  --<Company>FunctionAir</Company>
4 - Go to the project .csproj and run : dotnet pack / dotnet pack --configuration Release
5 - then go to the ./bin/debug folder where your .nupkg available and run : dotnet nuget push --source "EasyAccess" --api-key az .\EasyAccess.1.0.1.nupkg -->

# How to install Github Nuget Package

This is a readme file to understand how to install Github nuget packages.

## How to Authenticate.
To download a NuGet package from GitHub Packages, you need to configure your NuGet client to use the GitHub Packages source and provide authentication. Here's how to do that:

1. **Configure the NuGet client with your GitHub Packages feed**: You need to add the GitHub Packages feed to your NuGet configuration. This is usually done by adding a nuget.config file to your solution directory or modifying the global NuGet configuration on your machine.

2. **Authenticate with GitHub Packages**: You need to authenticate your NuGet client with GitHub by providing either a personal access token (PAT) with at least ***'read:packages'*** scope or using the **GITHUB_TOKEN** if you are fetching packages as part of a GitHub Actions workflow.

Here are the steps to configure NuGet to work with GitHub Packages:


## Step 1: Create a Personal Access Token (PAT)


1. Go to your GitHub settings.

2. Under "Developer settings," click on "Personal access tokens."
Click "Generate new token."

3. Give your token a name, select the ***'read:packages'*** scope to download packages.

4. Click **"Generate token"** and copy the generated token immediately as you won't be able to see it again.

## Step 2: Add the GitHub Packages source to your NuGet configuration

Create a ***'nuget.config'*** file in your solution directory with the following content:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="bium" value="https://nuget.pkg.github.com/foa-functionair/index.json" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
    <add key="bium.local" value="%userprofile%\.nuget\bium" />
  </packageSources>
</configuration>
```

Replace **GITHUB_USERNAME** with your GitHub username, and **GITHUB_TOKEN** with the personal access token you created.

## Step 3: Downloading the Package

Once your nuget.config is configured, you can download the package using the dotnet CLI or the NuGet CLI:

```sh
dotnet add package BiUM.Core --source "https://nuget.pkg.github.com/FOA-FunctiOnAir/index.json" --project .\PROJECT_PATH\
```

**Note**: If you're using this configuration on a build server or any kind of automation, make sure you protect your personal access token. If you are doing this as part of a GitHub Actions workflow, you can use the GITHUB_TOKEN instead of a PAT for authentication within the workflow.

# Project Documentation

- [BiUM.Bolt](src/BiUM.Bolt/README.md) - Database configuration and initialization.
- [BiUM.Contract](src/BiUM.Contract/README.md) - Shared gRPC contracts.
- [BiUM.Core](src/BiUM.Core/README.md) - Core utilities and shared services.
- [BiUM.Infrastructure](src/BiUM.Infrastructure/README.md) - Infrastructure implementations (gRPC, Redis, RabbitMQ, Serilog).
- [BiUM.Specialized](src/BiUM.Specialized/README.md) - High-level application configuration and specialized services.
