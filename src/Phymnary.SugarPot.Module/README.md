# Phymnary.SugarPot.Module

Organize startup code into modules and bind configuration sections to typed, validated options.

```bash
dotnet add package Phymnary.SugarPot.Module
```

It depends on [`Phymnary.SugarPot.DependencyInjection`](../Phymnary.SugarPot.DependencyInjection/README.md).

## Modules

A module groups related service registrations:

```csharp
using Phymnary.SugarPot.DependencyInjection;
using Phymnary.SugarPot.Module;
using Phymnary.SugarPot.Module.Extensions;

[Auto] // optional: also registers every [Service] class in this project
public partial class BillingModule : IModule
{
    public void ConfigureServices(IServiceCollection services, IConfigurationManager configuration)
    {
        services.ConfigureSection<StripeOptions, StripeOptionsValidator>();
    }
}
```

```csharp
var builder = WebApplication.CreateBuilder(args);   // any IHostApplicationBuilder
builder.AddModule<BillingModule>();
```

`AddModule<T>()` creates the module with `new()` and calls `ConfigureServices`. If the module implements `IAutoRegister`, as `[Auto]` modules do, it also calls `AddDependencies`.

## Typed configuration sections

Implement `IAppSettingsSection` and give it the section name:

```csharp
public class StripeOptions : IAppSettingsSection
{
    public static string Section => "Stripe";
    public string ApiKey { get; set; } = "";
}
```

```json
{ "Stripe": { "ApiKey": "sk_test_..." } }
```

Bind it, optionally with an `IValidateOptions<T>` validator:

```csharp
services.ConfigureSection<StripeOptions>();
services.ConfigureSection<StripeOptions, StripeOptionsValidator>();

// Read the bound values immediately, during startup:
services.ConfigureSection<StripeOptions>(opts => Console.WriteLine(opts.ApiKey));
```

Then inject `IOptions<StripeOptions>` as usual.

> `ConfigureSection` reads `IConfiguration` from the service collection. `HostApplicationBuilder` and `WebApplicationBuilder` register it automatically. In a bare `ServiceCollection`, register an `IConfiguration` instance first.

## Extension helpers

| Extension | Namespace | Description |
|---|---|---|
| `PascalToKebabCase()` | `Phymnary.SugarPot.AspNetCore.Extensions` | `OrderItems` to `order-items` |
| `IsBlank()`, `NullIfBlank()`, `TryGetValuable(out)` | same | Null or whitespace checks |
| `IsTruthy()` | same | `"true"`, `"1"`, `"t"` (any case) |
| `SplitBySemicolon()` | same | Split, trim, and drop empty entries |
| `StripPrefix()`, `StripPostfix()` | same | Remove a leading or trailing string or char |
| `Guid?.NullIfEmpty()`, `Guid?.TryGetValuable(out)` | `Phymnary.SugarPot.Module.Extensions` | Treat `Guid.Empty` as missing |
| `TryGetExplicit(out int)` | same | Enum to int, returns whether the value is defined |
| `AddIfNotExisted()` | same | Add to a collection only if the item is not already there |
| `AppendIfNotNull()`, `WhereNotNull()`, `JoinAsString()`, `IndexOf()` | same | Sequence helpers |
| `HasAttribute<T>()` | same | Reflection attribute check |
| `GetConfiguration()` | same | Get `IConfiguration` from an `IServiceCollection` |
