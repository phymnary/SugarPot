# Phymnary.SugarPot.DependencyInjection

Attribute-based service registration for `Microsoft.Extensions.DependencyInjection`. The registration code is generated at build time, so there is no reflection or assembly scanning at runtime.

```bash
dotnet add package Phymnary.SugarPot.DependencyInjection
```

The source generator ships inside the package.

## Usage

### 1. Mark services

```csharp
using Phymnary.SugarPot.DependencyInjection;

public interface IEmailSender { }

[Service(Lifetime.Scoped)]
public class SmtpEmailSender : IEmailSender { }       // registered as IEmailSender

[Service(Lifetime.Singleton, IsSelf = true)]
public class CacheWarmer : IHostedService { ... }    // registered as CacheWarmer
```

### 2. Create a registrar

Put `[Auto]` on a `partial` class. The generator makes it implement `IAutoRegister` and fills `AddDependencies` with every `[Service]` class in the project:

```csharp
[Auto]
public partial class AppServices;

new AppServices().AddDependencies(builder.Services);
```

With the [Module](../Phymnary.SugarPot.Module/README.md) package, put `[Auto]` on your module. `AddModule<T>()` then calls `AddDependencies` for you.

## How the service type is chosen

| Condition | Registered as |
|---|---|
| `IsSelf = true` | The class itself |
| The class declares interfaces | The **first** interface declared on the class |
| Only the base class declares interfaces | The first interface of the direct base class |
| No interfaces | The class itself |

The generated code is `services.Add{Lifetime}<TService, TImplementation>()`.

## Rules

- `[Service]` classes must not be abstract or generic. Otherwise you get build error `SPDI0001`.
- The `[Auto]` class must be `partial`. Otherwise it is skipped.
- Only the first interface is registered. To register more than one interface, implement `IAutoRegister` by hand.

## API

| Type | Description |
|---|---|
| `ServiceAttribute(Lifetime)` | Marks a class for registration. `IsSelf` registers the concrete type |
| `Lifetime` | `Singleton`, `Scoped`, `Transient` |
| `AutoAttribute` | Generates an `IAutoRegister` implementation on a partial class |
| `IAutoRegister` | `void AddDependencies(IServiceCollection services)`. You can also implement it by hand |
