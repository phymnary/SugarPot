# Phymnary.SugarPot.AspNetCore.Host

One extension method that loads configuration in the same order for every SugarPot host.

```bash
dotnet add package Phymnary.SugarPot.AspNetCore.Host
```

## Usage

```csharp
using Phymnary.SugarPot.AspNetCore.Extensions;

var builder = WebApplication.CreateBuilder(args);       // or Host.CreateApplicationBuilder(args)
builder.Configuration.AddDefaults(builder.Environment.EnvironmentName);
```

## Load order

When the same key appears in more than one source, the later source wins:

| # | Source | Required |
|---|---|---|
| 1 | `appsettings.json` | Yes |
| 2 | `appsettings.{env}.json` | No |
| 3 | `appsettings.{env}.user.json` | No. Use it for local developer overrides and keep it out of git |
| 4 | Environment variables | — |

`AddDefaults` appends these sources after any that are already registered. Sources added later, such as command-line arguments, override it. Sources added earlier are overridden by it.

Suggested `.gitignore` entry:

```gitignore
appsettings.*.user.json
```
