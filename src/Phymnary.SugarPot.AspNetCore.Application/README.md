# Phymnary.SugarPot.AspNetCore.Application

Application-layer helpers for SugarPot: a per-scope timestamp (`IRunAt`) and exceptions that map to HTTP status codes.

```bash
dotnet add package Phymnary.SugarPot.AspNetCore.Application
```

It depends on [`AspNetCore.Domain`](../Phymnary.SugarPot.AspNetCore.Domain/README.md) and the `Microsoft.AspNetCore.App` framework. [`AspNetCore.Api`](../Phymnary.SugarPot.AspNetCore.Api/README.md) references it, so you get it automatically with that package.

## `IRunAt`: one timestamp per request

```csharp
using Phymnary.SugarPot.AspNetCore.Extensions;

builder.Services.AddApplicationServices(); // registers IRunAt as scoped
```

`IRunAt.Value` is `DateTimeOffset.UtcNow`, read once when the service is first resolved in a scope. Everything in the same request sees the same time. The EF Core package uses it for `CreatedAt`, `UpdatedAt`, and `DeletedAt`.

```csharp
public class InvoiceService(IRunAt runAt)
{
    public Invoice Create() => new() { IssuedAt = runAt.Value };
}
```

## Exceptions

All of these implement `IBusinessException`, so the Api package's exception handler returns them with the matching status code.

| Exception | Status |
|---|---|
| `AspBadRequestException` | 400 Bad Request |
| `AspUnauthorizedException` | 401 Unauthorized |
| `AspForbiddenEndpointException` | 403 Forbidden |
| `AspInvalidOperationException` | 422 Unprocessable Entity |
| `InternalServiceUnavailableException` | 503 Service Unavailable |

Each one takes `(string message, Exception? innerException = null)` and supports a fluent error code:

```csharp
using Phymnary.SugarPot.AspNetCore.Exceptions;

if (string.IsNullOrWhiteSpace(request.Name))
    throw new AspBadRequestException("Name is required").WithErrorCode("NAME_REQUIRED");
```

## Claim names

`AspClaimNames` (namespace `Phymnary.SugarPot.AspNetCore.Security`) holds the claim types the Api package reads:

- `UserId` is `"sub"`.
- `TenantId` is `"tid"`.

Use these constants when you issue tokens.
