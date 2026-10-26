# Phymnary.SugarPot.AspNetCore.Domain

Contracts and base types for your domain layer: entities, repositories, validation, auditing, multi-tenancy, and business exceptions.

The package contains no persistence or HTTP code. Implementations live in [`AspNetCore.EntityFrameworkCore`](../Phymnary.SugarPot.AspNetCore.EntityFrameworkCore/README.md), [`AspNetCore.Application`](../Phymnary.SugarPot.AspNetCore.Application/README.md), and [`AspNetCore.Api`](../Phymnary.SugarPot.AspNetCore.Api/README.md).

```bash
dotnet add package Phymnary.SugarPot.AspNetCore.Domain
```

## Entities

Inherit from `Entity<TKey>`. It provides a `[Key] Id` and a `DomainStatus` object that the EF Core package reads at save time.

```csharp
using Phymnary.SugarPot.AspNetCore.Entities;

public class Order : Entity<Guid>
{
    private Order() { }                 // for EF Core
    public Order(Guid id) : base(id) { }

    public List<OrderLine> Lines { get; } = [];
}
```

### Adding child entities

Add children through `Attach`, which marks them as new. The repository then inserts them when you call `UpdateAsync` on the parent:

```csharp
using Phymnary.SugarPot.AspNetCore.Extensions;

order.Lines.Attach(new OrderLine(Guid.NewGuid()) { Sku = "A-1" });
await orderRepository.UpdateAsync(order);
```

### Opt-in behaviors

Implement these interfaces to turn on behaviors in the EF Core package:

| Interface | Members you implement | Effect (when enabled in EF Core) |
|---|---|---|
| `ISoftDelete` | `DeletedById`, `DeletedAt` | `repository.Delete()` sets these fields instead of removing the row; soft-deleted rows are filtered out of queries |
| `IAuditable` (`Auditings` namespace) | `GetAuditKey()`, `CreatedAt`, `CreatedById`, `UpdatedAt`, `UpdatedById` | Created/updated fields are stamped on save; property changes can be logged |
| `IMultiTenant` (`MultiTenancy` namespace) | `TenantId` | `TenantId` is set from `ICurrentTenant` on insert; queries are filtered by tenant |

## Validation

Implement `IEntityValidator<TEntity>` and return it from your repository options. The repository runs it before every insert, upsert, and update. A failure throws `EntityValidationException` with the details.

```csharp
public class OrderValidator : IEntityValidator<Order>
{
    public ValueTask<EntityValidationResult> ValidateAsync(Order order, CancellationToken ct = default)
    {
        if (order.Lines.Count > 0)
            return ValueTask.FromResult(EntityValidationResult.Valid);

        return ValueTask.FromResult(new EntityValidationResult
        {
            IsValid = false,
            Errors = [new EntityValidationFailureDetail
            {
                Property = nameof(Order.Lines),
                Message = "An order needs at least one line.",
                Code = "ORDER_EMPTY",
            }],
        });
    }
}
```

## Repositories

`IRepository<TEntity>` (namespace `Repositories`):

| Method | Description |
|---|---|
| `InsertAsync(entity, autoSave = true)` | Validate, add, and save |
| `UpsertAsync(entity, on, autoSave = true, canIncludeDetails = false)` | Update the first match of `on` if one exists; otherwise insert |
| `UpdateAsync(entity)` | Validate and save changes to a tracked entity |
| `Delete(entity)` | Soft-delete for `ISoftDelete`, otherwise remove. Saved on the next `UpdateAsync`/`InsertAsync` |
| `FindAsync(predicate, canIncludeDetails = false)` | First match or `null` |
| `QueryAsync(predicate = null, canIncludeDetails = false)` | List of matches |
| `AnyAsync(...)` / `CountAsync(...)` | Existence and count, with an optional predicate |
| `AdvanceQuery(filter = null, canIncludeDetails = null)` | Start a staged query (see below) |

`IRepository<TEntity, TKey>` adds `GetAsync(id, canIncludeDetails = false)`, which throws `EntityNotFoundException` when no row matches.

### Advanced queries

The builder goes through each stage in order: **filter → order → page → (optional) project → run**.

```csharp
using Phymnary.SugarPot.AspNetCore.Repositories.AdvanceQueries;

PaginateResult<OrderSummary> page = await orders
    .AdvanceQuery(q => q.Where(o => o.Lines.Count > 0))
    .OrderByDescending(o => o.Id)
    .Pick(perPage: 20, pageIndex: 1)            // pageIndex starts at 1; 0 = no skip
    .Select(o => new OrderSummary(o.Id, o.Lines.Count))
    .PaginateAsync();

int total = page.Count;                         // count of the filtered query, before paging
await foreach (var item in page.Items) { /* ... */ }
```

Call `.Build()` instead of `PaginateAsync()` to get the items without a count. Pass `perPage: int.MaxValue` to skip `Take`.

## Request and scope context

| Contract | Purpose | Default implementation |
|---|---|---|
| `ICurrentUser` (`Security`) | `Guid? Id` of the caller | `AspNetCore.Api` |
| `ICurrentTenant` (`MultiTenancy`) | `Guid? Id` of the active tenant | `AspNetCore.Api` |
| `IAbortedToken` | `Get(ct)` returns `ct`, or the request-aborted token when `ct` is `default` | `AspNetCore.Api` |
| `IRunAt` | One fixed `DateTimeOffset` per DI scope | `AspNetCore.Application` |
| `IScopeBuilder` | Creates a new scope that keeps the user, tenant, and token | `AspNetCore.Api` |
| `IDbFunctionProvider` | Transactions and EF execution strategies | `AspNetCore.EntityFrameworkCore` |

To run background work as the current user and tenant:

```csharp
using Phymnary.SugarPot.AspNetCore.Api.Extensions; // note: namespace differs from the package

await using var scope = serviceProvider.InheritAsyncServiceScope();
var repo = scope.ServiceProvider.GetRequiredService<OrderRepository>();
```

## Auditing contracts

These types configure which properties the EF Core package logs. They live in the `Auditings` namespace.

| Type | Use |
|---|---|
| `[AuditOnly("Name", "Price")]` | On a class: log only the listed properties |
| `[DisabledAuditing]` | On a property: never log it |
| `IPropertyChangeAudit` | Shape of an audit row: entity name/id, property, type, old/new JSON values, who, when |
| `PropertyChangeAuditHydration` | Concrete `IPropertyChangeAudit` passed to your mapper |
| `TrackBy` | `Domain` (class/property names) or `Database` (table/column names) |

## Exceptions

Every exception implements `IBusinessException` (`StatusCode`, `ErrorCode`). The Api package's exception handler turns them into JSON responses.

| Exception | Status | Thrown by the library when |
|---|---|---|
| `EntityNotFoundException` | 404 | `GetAsync` finds nothing |
| `EntityValidationException` | 400 | A validator fails (`Failures` holds the details) |
| `EntityPersistenceException` | 409 | EF Core throws `DbUpdateException` |
| `TenantMissingInContextException` | 403 | A multi-tenant entity is inserted with no current tenant |
| `DomainNotImplementedException` | 422 | A required option is missing, for example an upsert without `UpdateOptions.Update` |

Set an error code per throw with `.WithErrorCode("CODE")`. To set defaults for every throw, use `DomainErrorCodeRegistry`:

```csharp
DomainErrorCodeRegistry.DefaultEntityNotFoundErrorCode = "NOT_FOUND";
```

To create your own domain exceptions, implement `IDomainException`.
