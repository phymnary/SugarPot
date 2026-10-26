# Phymnary.SugarPot.AspNetCore.Api

API-layer package for SugarPot. It contains:

- A **source generator** that turns `[Endpoint]` classes into minimal-API routes.
- **Request context binding** that fills `ICurrentUser`, `ICurrentTenant`, and `IAbortedToken` from `HttpContext`.
- A **JSON exception handler** for `IBusinessException`.

```bash
dotnet add package Phymnary.SugarPot.AspNetCore.Api
```

This package also brings in [`AspNetCore.Application`](../Phymnary.SugarPot.AspNetCore.Application/README.md) and [`AspNetCore.Domain`](../Phymnary.SugarPot.AspNetCore.Domain/README.md). The generator ships inside the package, so you don't install anything else.

## Quick start

```csharp
using Phymnary.SugarPot.AspNetCore.Api.Extensions;
using Phymnary.SugarPot.AspNetCore.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApiServices()                   // ICurrentUser, ICurrentTenant, IAbortedToken, IScopeBuilder
    .AddApplicationServices()           // IRunAt (from the Application package)
    .AddBoilerplateExceptionHandler();  // ProblemDetails + AspExceptionHandler

var app = builder.Build();

app.UseExceptionHandler();
// If you call UseAuthentication() yourself, call it before this line.
app.UseBoilerplateServices();

app.MapEndpoint<GetHealth>();
app.Run();
```

```csharp
using Phymnary.SugarPot.AspNetCore.Api;

[Endpoint(Method.Get)]
public partial class GetHealth
{
    private static string RoutePattern => "/health";

    private static IResult HandleAsync() => Results.Ok(new { ok = true });
}
```

## Endpoints

Put `[Endpoint]` on a `partial` class. The generator implements `IEndpoint.ConfigureRouteBuilder`, which calls `app.Map{Method}(pattern, HandleAsync)`.

| Member | Required | Purpose |
|---|---|---|
| `HandleAsync` method | **Yes**. Without it you get build error `SPAPI001` | The route handler. It can be static or instance, with any minimal-API parameters |
| `RoutePattern` property | One of `RoutePattern` or a group `[RoutePattern]`. If neither exists, nothing is generated | Route template |
| `BuildRoute(RouteHandlerBuilder)` method | No | Customize the route: tags, auth, filters, and so on |

### HTTP method

The generator uses the first of these that applies:

1. The attribute argument: `[Endpoint(Method.Post)]`
2. A class name prefix: `GetBook`, `PostUser`, `DeleteOrder`, ...
3. The last namespace segment, case-insensitive: `MyApp.Books.GET`

If none of these applies, no code is generated for the class.

### Route groups

Instead of repeating patterns and builders on every endpoint, add a static class with `[RoutePattern]` and/or `[RouteBuilder]` methods. Each endpoint uses the group method whose namespace is the closest match to its own. A `RoutePattern` property or `BuildRoute` method on the endpoint takes precedence.

```csharp
namespace MyApp.Api;

public static class ApiGroup
{
    [RoutePattern]
    public static string Pattern<TEndpoint>(TEndpoint endpoint) where TEndpoint : class, IEndpoint
        => endpoint.GetRoutePatternBasedOnNamespace(root: "MyApp.Api", prefix: "/api");

    [RouteBuilder]
    public static RouteHandlerBuilder Build(RouteHandlerBuilder builder)
        => builder.RequireAuthorization();
}
```

`GetRoutePatternBasedOnNamespace` builds the route from the endpoint's namespace:

1. It removes `root` and any trailing `.GET`/`.POST`/... segment (upper case only).
2. It converts each remaining segment to kebab-case.
3. A segment wrapped in underscores becomes a route parameter.

For example, `MyApp.Api.OrderItems._Id_.GET` becomes `/api/order-items/{id}`.

### Map all endpoints at once

Put `[ApiSchema]` on a partial class. The generator adds a `MapEndpoints(IEndpointRouteBuilder app)` method that maps every `[Endpoint]` in the project. The method is `static` if the class is static.

```csharp
[ApiSchema]
public static partial class AppEndpoints;

AppEndpoints.MapEndpoints(app);
```

An endpoint with constructor parameters is resolved from DI, so you must register it. Endpoints with no constructor parameters are created with `new()`.

### Without the generator

Implement `IEndpoint` yourself and call `app.MapEndpoint<T>()`, or `app.MapEndpoint<T>(serviceProvider)` to resolve it from DI. If the endpoint also implements `IExtendRouteBuilder`, its `Extend(builder)` method runs after mapping.

## Request context

`UseBoilerplateServices()` adds a middleware that fills these scoped services on each request:

| Service | Source |
|---|---|
| `ICurrentUser.Id` | `sub` claim (`AspClaimNames.UserId`), parsed as `Guid` |
| `ICurrentTenant.Id` | `tid` claim (`AspClaimNames.TenantId`), parsed as `Guid` |
| `IAbortedToken` | `HttpContext.RequestAborted` |

Claims that are missing or are not GUIDs leave the value as `null`. The claim names are constants.

To run background work as the same user and tenant, call `serviceProvider.InheritAsyncServiceScope()` from the Domain package. It uses the `IScopeBuilder` registered here.

## Error responses

`AddBoilerplateExceptionHandler()` registers `AspExceptionHandler`, which answers with:

```json
{
  "error": {
    "message": "Could not find Book with id ...",
    "code": "NOT_FOUND",
    "detail": "<exception.ToString()>",
    "invalidParameters": [{ "property": "Title", "message": "...", "code": "..." }]
  }
}
```

- The status code comes from `IBusinessException.StatusCode`. Any other exception returns **500**.
- `invalidParameters` appears only for `EntityValidationException`. Null fields are left out.
- To localize messages, register an `IAspErrorMessageProvider`. When an exception has an error code, the handler calls `GetAsync(code)` and uses the result as `message`.

> `detail` includes the full exception text and stack trace in every environment. Check that this is acceptable before exposing the API publicly.
