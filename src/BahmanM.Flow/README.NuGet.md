# BahmanM.Flow

Composable workflows for .NET with structured concurrency: cancellation and time‑boxed scopes, fan‑out/fan‑in (Any/All), resource lifecycles, and typed outcomes.

## Install

-  .NET CLI: `dotnet add package BahmanM.Flow`
-  PackageReference: `<PackageReference Include="BahmanM.Flow" Version="x.y.z" />`

## Quick Start

```csharp
using BahmanM.Flow;

// Define a small workflow
var flow = Flow
    .Succeed("start")
    .Select(s => s.ToUpperInvariant())
    .Chain(upper => Flow.Succeed($"HELLO {upper}"))
    .WithTimeout(TimeSpan.FromSeconds(2));

// Execute
var outcome = await FlowEngine.ExecuteAsync(flow);

if (outcome is Success<string> s)
    Console.WriteLine(s.Value);
else if (outcome is Failure<string> f)
    Console.WriteLine(f.Exception.Message);
```

## Documentation

For guides and rationale, see the repository:

https://github.com/bahmanm/BahmanM.Flow
