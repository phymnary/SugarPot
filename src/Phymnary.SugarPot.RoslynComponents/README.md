# Phymnary.SugarPot.RoslynComponents

Shared helpers for writing incremental source generators and analyzers. The SugarPot generators (`AspNetCore.Api.Roslyn`, `DependencyInjection.Roslyn`) use it internally. Application code does not need it.

- Target: `netstandard2.0`, as Roslyn components require.
- Dependencies: `Microsoft.CodeAnalysis.CSharp`, `Microsoft.CodeAnalysis.Analyzers`.

```bash
dotnet add package Phymnary.SugarPot.RoslynComponents
```

## What's inside

| Namespace | Types | Use |
|---|---|---|
| `…RoslynComponents.Extensions` | `SymbolExtensions`, `AttributeDataExtensions`, `ProviderExtensions`, `CompilationExtensions`, ... | Symbol lookups by metadata name, attribute argument access, `GroupBy` for incremental pipelines |
| `…RoslynComponents.Models` | `HierarchyInfo`, `TypeInfo`, `SourceDiagnosticInfo`, `Result<T>` | Equatable, cache-friendly pipeline models |
| `…RoslynComponents.Helpers` | `EquatableArray<T>`, `ImmutableArrayBuilder<T>`, `ObjectPool<T>` | Low-allocation collections with value equality |

## Common tasks

**Check a symbol for an attribute or base type:**

```csharp
using Phymnary.SugarPot.RoslynComponents.Extensions;

bool isEndpoint = symbol.HasAttributeWithFullyQualifiedMetadataName("MyLib.EndpointAttribute");
bool isHandler  = type.ImplementsFromFullyQualifiedMetadataName("MyLib.IHandler");
string name     = type.GetFullyQualifiedName();   // "global::Ns.Type"
```

**Read attribute arguments:**

```csharp
bool isSelf = attribute.GetNamedArgument("IsSelf", false);
```

**Generate a partial type in the right namespace and nesting:**

```csharp
using Phymnary.SugarPot.RoslynComponents.Models;

var hierarchy = HierarchyInfo.From(typeSymbol);   // equatable, safe to cache in a pipeline
CompilationUnitSyntax unit = hierarchy.GetCompilationUnit(members, baseList);
context.AddSource($"{hierarchy.FilenameHint}.g.cs", unit);
```

**Report diagnostics from cached pipeline data:**

```csharp
var info = SourceDiagnosticInfo.Create(descriptor, symbol, symbol.Name);
context.ReportDiagnostic(info.ToDiagnostic());
```

## Guidelines

- Keep pipeline models equatable. Use `EquatableArray<T>` instead of `ImmutableArray<T>` so incremental caching works.
- Don't store `ISymbol` or `SyntaxNode` in models. Extract strings or `HierarchyInfo` instead.
