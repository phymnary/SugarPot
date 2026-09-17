# Phymnary.SugarPot.AspNetCore.Host

Host bootstrapping utilities for SugarPot ASP.NET Core applications.

This package provides a lightweight host-layer building block to keep startup behavior consistent across services.

## Architecture

`Phymnary.SugarPot.AspNetCore.Host` sits at the application host boundary and focuses on bootstrapping concerns only.

- Keeps configuration composition logic close to host startup
- Avoids leaking startup wiring into domain/application layers
- Provides a single extension point for shared service defaults across multiple apps

The package is intentionally minimal and can be used by both web hosts and generic hosts.

## What this package provides

- `ConfigurationExtensions.AddDefaults(IConfigurationBuilder builder, string env)`

This extension adds configuration sources in the following order:

1. `appsettings.json` (required)
2. `appsettings.{env}.json` (optional)
3. `appsettings.{env}.user.json` (optional)
4. Environment variables

Because later providers override earlier ones, environment variables remain the final override layer.

## Feature summary

- Deterministic configuration provider ordering
- Environment-aware `appsettings.{env}.json` loading
- Optional developer-local override file support (`appsettings.{env}.user.json`)
- Compatibility with standard ASP.NET Core host bootstrapping patterns

## Installation

```bash
dotnet add package Phymnary.SugarPot.AspNetCore.Host
```

## Usage

### WebApplication

```csharp
using Phymnary.SugarPot.AspNetCore.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddDefaults(builder.Environment.EnvironmentName);
```

### Generic Host

```csharp
using Microsoft.Extensions.Hosting;
using Phymnary.SugarPot.AspNetCore.Extensions;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddDefaults(builder.Environment.EnvironmentName);
```

## Why use it

- Standardizes configuration loading across services
- Supports environment-specific and user-local override files
- Preserves common ASP.NET Core environment variable override behavior

## .NET requirements (high-level)

- Use a supported modern .NET SDK used by this repository (currently .NET 8/9/10 family).
- This package relies on `Microsoft.AspNetCore.App` and is intended for ASP.NET Core host environments.

Basic local workflow:

```bash
dotnet restore
dotnet build
dotnet test
```

## Related packages

- Phymnary.SugarPot.AspNetCore.Api
- Phymnary.SugarPot.AspNetCore.EntityFrameworkCore

## Contributing

Issues and pull requests are welcome.

## License

See the repository root for license details.
