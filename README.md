# Phymnary.SugarPot

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

SugarPot is a set of small NuGet packages for building layered ASP.NET Core applications. Each package handles one concern: domain contracts, EF Core persistence, API wiring, configuration, or compile-time code generation. Use only the packages you need.

## What you get

- **Repositories**: `IRepository<TEntity>` with insert, upsert, update, delete, querying, and a staged builder for ordering and paging.
- **Save-time behaviors**: created/updated stamps, property-change audit logs, soft delete, and tenant assignment, applied when a repository saves.
- **Global query filters**: soft-deleted rows and rows from other tenants are filtered out automatically.
- **Validation and errors**: domain validators run before persistence. Business exceptions carry an HTTP status and error code, and a built-in handler writes them as JSON.
- **Request context**: the current user, tenant, and request-aborted token are read from `HttpContext` and can be passed to background scopes.
- **Source generators**: `[Endpoint]` classes become minimal-API routes, and `[Service]` classes register themselves in DI.
- **Modules and configuration**: `IModule` for grouping service registration, and `IAppSettingsSection` for typed, validated options.

## Packages

| Package | Use it for | Docs |
|---|---|---|
| `Phymnary.SugarPot.AspNetCore.Domain` | Entities, repository contracts, auditing/tenancy/soft-delete contracts, business exceptions | [README](src/Phymnary.SugarPot.AspNetCore.Domain/README.md) |
| `Phymnary.SugarPot.AspNetCore.Application` | `IRunAt` request timestamp and HTTP-aware application exceptions | [README](src/Phymnary.SugarPot.AspNetCore.Application/README.md) |
| `Phymnary.SugarPot.AspNetCore.EntityFrameworkCore` | EF Core repository base class, save-time behaviors, query-filter helper | [README](src/Phymnary.SugarPot.AspNetCore.EntityFrameworkCore/README.md) |
| `Phymnary.SugarPot.AspNetCore.Api` | Endpoint source generator, current user/tenant binding, JSON exception handler | [README](src/Phymnary.SugarPot.AspNetCore.Api/README.md) |
| `Phymnary.SugarPot.AspNetCore.Host` | Standard `appsettings` loading order | [README](src/Phymnary.SugarPot.AspNetCore.Host/README.md) |
| `Phymnary.SugarPot.DependencyInjection` | `[Service]` / `[Auto]` DI registration source generator | [README](src/Phymnary.SugarPot.DependencyInjection/README.md) |
| `Phymnary.SugarPot.Module` | `IModule`, typed configuration sections, small extension helpers | [README](src/Phymnary.SugarPot.Module/README.md) |
| `Phymnary.SugarPot.RoslynComponents` | Shared helpers for writing Roslyn generators (internal tooling) | [README](src/Phymnary.SugarPot.RoslynComponents/README.md) |

How the packages depend on each other:

```text
AspNetCore.Api ──► AspNetCore.Application ──► AspNetCore.Domain ◄── AspNetCore.EntityFrameworkCore
AspNetCore.Host        (standalone)
Module ──► DependencyInjection
```

Installing `AspNetCore.Api` also brings in `Application` and `Domain`.

## Getting started

This walkthrough builds a web API with one entity backed by EF Core.

### 1. Install

```bash
dotnet add package Phymnary.SugarPot.AspNetCore.Api
dotnet add package Phymnary.SugarPot.AspNetCore.EntityFrameworkCore
dotnet add package Phymnary.SugarPot.AspNetCore.Host
dotnet add package Microsoft.EntityFrameworkCore.SqlServer   # or your provider
```

### 2. Define an entity

```csharp
using Phymnary.SugarPot.AspNetCore.Auditings;
using Phymnary.SugarPot.AspNetCore.Entities;

public class Book : Entity<Guid>, IAuditable, ISoftDelete
{
    private Book() { }                       // for EF Core
    public Book(Guid id) : base(id) { }

    public required string Title { get; set; }

    // IAuditable
    public string GetAuditKey() => Id.ToString();
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedById { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedById { get; set; }

    // ISoftDelete
    public Guid? DeletedById { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
```

### 3. Create the DbContext and repository

```csharp
using Microsoft.EntityFrameworkCore;
using Phymnary.SugarPot.AspNetCore;
using Phymnary.SugarPot.AspNetCore.Repositories;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Adds the soft-delete query filter to Book
        new ModelBuilderHelper(modelBuilder).BuildEntity<Book>();
    }
}

public class BookRepositoryOptions : EfRepositoryOptions<Book>;

public class BookRepository(AppDbContext db, IRepositoryOptions<Book> options, EfRepositoryAddons addons)
    : EfRepository<AppDbContext, Book, Guid>(db, options, addons);
```

### 4. Wire up `Program.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Phymnary.SugarPot.AspNetCore.Api.Extensions;
using Phymnary.SugarPot.AspNetCore.Extensions;
using Phymnary.SugarPot.AspNetCore.Repositories;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddDefaults(builder.Environment.EnvironmentName);

builder.Services
    .AddApiServices()                   // ICurrentUser, ICurrentTenant, IAbortedToken, IScopeBuilder
    .AddApplicationServices()           // IRunAt
    .AddBoilerplateExceptionHandler()   // JSON error responses
    .AddDbContext<AppDbContext>(o => o.UseSqlServer(builder.Configuration.GetConnectionString("Default")))
    .AddEfCoreServices<AppDbContext>(ef => ef.AddSoftDelete())
    .AddScoped<IRepositoryOptions<Book>, BookRepositoryOptions>()
    .AddScoped<BookRepository>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseBoilerplateServices();           // reads "sub" / "tid" claims into ICurrentUser / ICurrentTenant

app.MapEndpoint<GetBook>();
app.Run();
```

### 5. Add an endpoint

```csharp
using Phymnary.SugarPot.AspNetCore.Api;

[Endpoint(Method.Get)]
public partial class GetBook
{
    private static string RoutePattern => "/books/{id:guid}";

    private static async Task<Book> HandleAsync(Guid id, BookRepository books) =>
        await books.GetAsync(id);   // throws EntityNotFoundException -> 404 JSON response
}
```

The source generator implements `IEndpoint` for `GetBook`. See the [Api README](src/Phymnary.SugarPot.AspNetCore.Api/README.md) for route groups and `[ApiSchema]`, which maps every endpoint with one call.

## Supported frameworks

The runtime packages target `net8.0`, `net9.0`, and `net10.0`. `RoslynComponents` targets `netstandard2.0`.

The EF Core package depends on EF Core `[8.0.0,)` for net8/net9 and `[10.0.0,)` for net10.

## Building from source

You need the .NET 10 SDK (it builds all three targets and reads the `.slnx` solution).

```bash
git clone https://github.com/phymnary/SugarPot.git
cd SugarPot
dotnet build Phymnary.SugarPot.slnx
dotnet test Phymnary.SugarPot.slnx
```

A `Release` build produces NuGet packages, because `GeneratePackageOnBuild` is on in Release. The `Api` and `DependencyInjection` packages include their Roslyn generators under `analyzers/dotnet/cs`.

```bash
dotnet build Phymnary.SugarPot.slnx -c Release
```

Repository layout:

| Path | Contents |
|---|---|
| `src/` | Packable libraries |
| `src/roslyn/` | Source generators and analyzers, packed inside `Api` and `DependencyInjection` |
| `test/` | xUnit tests: EF Core behaviors and generator output |
| `Directory.Build.props` | Package versions: `AspPackedVersion` for the `AspNetCore.*` packages, `DIPackedVersion` |
| `Directory.Packages.props` | Central NuGet package versions |

## License

[MIT](LICENSE)
