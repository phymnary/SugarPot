# Phymnary.SugarPot.AspNetCore.EntityFrameworkCore

EF Core implementation of the SugarPot [Domain](../Phymnary.SugarPot.AspNetCore.Domain/README.md) contracts. It includes a repository base class, save-time behaviors (child-entity attach, soft delete, tenant assignment, auditing), query-filter setup, and transaction helpers.

```bash
dotnet add package Phymnary.SugarPot.AspNetCore.EntityFrameworkCore
```

EF Core dependency: `[8.0.0,)` on net8.0/net9.0 and `[10.0.0,)` on net10.0. Add your own database provider package.

## Setup

### 1. Register services

```csharp
using Phymnary.SugarPot.AspNetCore.Extensions;

services.AddDbContext<AppDbContext>(o => o.UseSqlServer(connectionString));

services.AddEfCoreServices<AppDbContext>(ef => ef
    .AddSoftDelete()      // optional
    .AddMultiTenancy()    // optional
    .AddAuditing(audit => audit.AddPropertyChangeAudit(MapAudit))); // optional, see "Auditing"
```

You don't add EF interceptors to the `DbContext`. These behaviors run inside `EfRepository` just before it calls `SaveChangesAsync`.

> **Save through the repository.** If you call `dbContext.SaveChangesAsync()` directly, none of these behaviors run.

`AddEfCoreServices` also registers `IDbFunctionProvider`, `EfDbStateManager`, and `EfRepositoryAddons`.

**Your app must provide** `ICurrentUser`, `IRunAt`, and `IAbortedToken`, plus `ICurrentTenant` when multi-tenancy is on. `AddApiServices()` and `AddApplicationServices()` from the Api and Application packages register all of them.

### 2. Configure the model

`ModelBuilderHelper` adds global query filters for `ISoftDelete` and `IMultiTenant` entities and stops EF from mapping `DomainStatus`:

```csharp
using Phymnary.SugarPot.AspNetCore;

public class AppDbContext(DbContextOptions<AppDbContext> options, ICurrentTenant tenant) : DbContext(options)
{
    private Guid TenantId => tenant.Id ?? Guid.Empty;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        new ModelBuilderHelper(modelBuilder) { TenantIdAccessor = () => TenantId }
            .BuildEntity<Book>(b => b.Property(x => x.Title).HasMaxLength(200), schema: "app")
            .BuildEntity<Order>();
    }
}
```

| Entity implements | Filter added |
|---|---|
| `ISoftDelete` | `DeletedAt == null` |
| `IMultiTenant` | `TenantId == TenantIdAccessor()` |

- On **net10.0** the filters are named (`ModelBuilderHelper.SoftDelelteQueryFilterName`, `MultiTenancyQueryFilterName`), so you can turn them off one at a time with `IgnoreQueryFilters([...])`. On net10.0, a multi-tenant entity without a `TenantIdAccessor` throws.
- On **net8.0/net9.0** both conditions are combined into one filter. The tenant condition is skipped if there is no `TenantIdAccessor`.
- `BuildEntity` adds filters and runs your `additionalConfigure` callback. It does not set table names. The `schema` argument is accepted but not applied, so set it with `b.ToTable(...)` inside the callback.

### 3. Create repositories

Each repository needs an `IRepositoryOptions<TEntity>` registration, even an empty one:

```csharp
using Phymnary.SugarPot.AspNetCore.Repositories;

public class BookRepositoryOptions : EfRepositoryOptions<Book>;

public class BookRepository(AppDbContext db, IRepositoryOptions<Book> options, EfRepositoryAddons addons)
    : EfRepository<AppDbContext, Book, Guid>(db, options, addons);

services
    .AddScoped<IRepositoryOptions<Book>, BookRepositoryOptions>()
    .AddScoped<BookRepository>();
```

Use `EfRepository<TDbContext, TEntity>` for entities without an `Entity<TKey>` key; it doesn't have `GetAsync`. Subclasses can use the protected `DbSet`, `Queryable(canIncludeDetails)`, `ValidateAndThrowAsync`, and `GetRequestAborted` members.

## Repository options

Options control includes, upsert merging, delete behavior, and validation for each entity type:

```csharp
using Phymnary.SugarPot.AspNetCore.Extensions; // IncludeIn

public class OrderRepositoryOptions : EfRepositoryOptions<Order>
{
    public OrderRepositoryOptions()
    {
        Validator = new OrderValidator();

        QueryOptions = new EntityQueryOptions<Order>
        {
            DefaultIncludeQuery = q => q.Include(o => o.Customer),   // used by every read
            IncludeDetailsQuery = q => q.IncludeIn(o => o.Lines),    // added when canIncludeDetails: true
        };

        UpdateOptions = new EntityUpdateOptions<Order>
        {
            Update = (incoming, existing) => existing.Note = incoming.Note, // required for UpsertAsync
            OnDelete = order => false,                                      // return true to skip the default delete
        };
    }
}
```

The default includes are applied by `FindAsync`, `QueryAsync`, `GetAsync`, and `UpsertAsync`, and by `AdvanceQuery` when you pass `canIncludeDetails`. `AnyAsync` and `CountAsync` query the `DbSet` directly.

## What happens on save

`InsertAsync`, `UpsertAsync`, and `UpdateAsync` run these steps in order:

1. **Validate**: `IRepositoryOptions.Validator`, if one is set.
2. **Attach** (always on): children added with `collection.Attach(child)` are marked `Added`.
3. **Soft delete** (`AddSoftDelete`): entities flagged by `Delete()` get `DeletedAt` from `IRunAt` and `DeletedById` from `ICurrentUser`.
4. **Tenant** (`AddMultiTenancy`): new `IMultiTenant` entities get `TenantId` from `ICurrentTenant`. If there is no tenant, it throws `TenantMissingInContextException`.
5. **Audit** (`AddAuditing`): see [Auditing](#auditing).
6. **Save**: `DbUpdateException` is rethrown as `EntityPersistenceException` (409).

> Without `AddSoftDelete()`, calling `Delete()` on an `ISoftDelete` entity only flags it, and nothing is written.

## Auditing

With `AddAuditing`, every `IAuditable` entity gets:

- **On insert:** `CreatedAt` and `CreatedById`.
- **On update** (including changes inside `[Owned]` types): `UpdatedAt` and `UpdatedById`, plus one audit row per changed property, if you registered a property-change audit.

### Store property changes

Define an audit entity, add it to your `DbContext`, and pass a mapper:

```csharp
using Phymnary.SugarPot.AspNetCore.Auditings;

public class PropertyChangeLog : Entity<Guid>, IPropertyChangeAudit
{
    public PropertyChangeLog(Guid id) : base(id) { }
    public required string EntityName { get; set; }
    public required string EntityId { get; set; }
    public required string PropertyName { get; set; }
    public required string TypeName { get; set; }
    public required string OldValue { get; set; }   // JSON
    public required string NewValue { get; set; }   // JSON
    public Guid? ModifiedById { get; set; }
    public DateTimeOffset ModifiedAt { get; set; }
}

services.AddEfCoreServices<AppDbContext>(ef => ef.AddAuditing(audit => audit
    .ConfigureStructure(s => s.TrackBy = TrackBy.Domain)   // or TrackBy.Database
    .AddPropertyChangeAudit(h => new PropertyChangeLog(Guid.NewGuid())
    {
        EntityName = h.EntityName, EntityId = h.EntityId, PropertyName = h.PropertyName,
        TypeName = h.TypeName, OldValue = h.OldValue, NewValue = h.NewValue,
        ModifiedById = h.ModifiedById, ModifiedAt = h.ModifiedAt,
    })));
```

Always pass the mapper. The change tracker needs it to build audit rows.

### Choose which properties are logged

| Rule | Result |
|---|---|
| `[AuditOnly("Title", "Price")]` on the entity | Only these properties are logged |
| `[DisabledAuditing]` or `[NotMapped]` on a property | Never logged |
| Reference to another class | Logged as `{Name}{IdPostfix}`, for example `CustomerId` (`IdPostfix` defaults to `"Id"`) |
| Property of an `[Owned]` type | Logged as `Owner.Property`, for example `Address.City` |

`ConfigureStructure` options:

- `TrackBy.Domain` (the default) logs CLR type and property names.
- `TrackBy.Database` logs table and column names.
- `IdPostfix` changes the suffix used for reference properties.

### Separate audit database

To write audit rows to a different `DbContext`, use the generic overload:

```csharp
ef.AddAuditing<AuditDbContext>(audit => audit.AddPropertyChangeAudit(MapAudit));
```

Only one `AddAuditing` call is allowed per `AddEfCoreServices`.

## Transactions and resilience

Inject `IDbFunctionProvider`:

```csharp
await using var tx = await db.BeginTransactionAsync();
// ... repository calls ...
await tx.CommitAsync();

// Retry with the provider's execution strategy
await db.UseResilientStrategyAsync(async ct => { /* ... */ });

// Retry inside a transaction; verifySucceeded is called if the commit outcome is unknown
await db.UseResilientStrategyWithTransactionAsync(
    async ct => { /* ... */ },
    async ct => await repo.AnyAsync(x => x.Id == id, ct));
```

`IQueryTransaction` mirrors EF's `IDbContextTransaction` and supports savepoints. Every cancellation token falls back to the request-aborted token.
