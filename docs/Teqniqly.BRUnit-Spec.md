# Teqniqly.BRUnit – Specification

## 1. Overview

Teqniqly.BRUnit is a .NET library suite that enables executing Bruno (`bru`) API contract test collections
inside .NET integration test pipelines. It bridges application-level integration testing and contract testing by
allowing `.bru` files to run against ephemeral WebApplicationFactory-driven test servers.

The library is designed with strict separation of concerns:

| Layer                                | Purpose                                                  |
| ------------------------------------ | -------------------------------------------------------- |
| `Teqniqly.BRUnit.Testing`            | Core Bruno runner — **framework agnostic**               |
| `Teqniqly.BRUnit.Testing.AspNetCore` | Host integration layer — accepts `WebApplicationFactory` |
| `Teqniqly.BRUnit.Xunit`              | Convenience wrapper — adds xUnit fixture integration     |

Future frameworks (NUnit/MSTest) can be added without changing core packages.

---

## 2. Goals

1. Provide a **test-framework agnostic core runner** for invoking Bruno CLI.
2. Enable Bruno collections to run against **WebApplicationFactory** test servers.
3. Allow xUnit users to adopt with **one base class**, no ceremony.
4. Keep architecture future-proof for NUnit, MSTest, SpecFlow, etc.
5. Make failures appear as **standard test failures in `dotnet test`**.

---

## 3. Package Breakdown

### 3.1 Core Package – `Teqniqly.BRUnit.Testing`

✔ No dependency on testing frameworks  
✔ Runs `bru` via process execution  
✔ Foundation for all higher layers

Includes:

- `BrunoRunOptions`
- `BrunoRunResult`
- `IBrunoRunner`
- `BrunoRunner` CLI executor

### 3.2 ASP.NET Host Support – `Teqniqly.BRUnit.Testing.AspNetCore`

✔ Accepts `WebApplicationFactory<TEntryPoint>`  
✔ Base class for host-backed Bruno execution  
✔ Still test framework independent

### 3.3 xUnit Integration – `Teqniqly.BRUnit.Xunit`

✔ Wraps ASP.NET layer  
✔ Implements `IClassFixture`  
✔ Streams stdout/stderr to xUnit output reporter

Future siblings could be:

- `Teqniqly.BRUnit.NUnit`
- `Teqniqly.BRUnit.MSTest`

---

## 4. Core API – `Teqniqly.BRUnit.Testing`

### `BrunoRunOptions`

```csharp
public sealed class BrunoRunOptions
{
    public string BruExecutablePath { get; init; } = "bru";
    public string WorkingDirectory { get; init; } = Directory.GetCurrentDirectory();
    public string Target { get; init; } = string.Empty; // .bru file or folder
    public string? EnvironmentName { get; init; }
    public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(2);
    public IDictionary<string,string?> EnvironmentVariables { get; init; }
        = new Dictionary<string,string?>(StringComparer.OrdinalIgnoreCase);
}
```

### `BrunoRunResult`

```csharp
public sealed class BrunoRunResult
{
    public int ExitCode { get; init; }
    public string StandardOutput { get; init; } = "";
    public string StandardError { get; init; } = "";
    public bool IsSuccess => ExitCode == 0;
}
```

### `IBrunoRunner` + `BrunoRunner`

```csharp
public interface IBrunoRunner
{
    Task<BrunoRunResult> RunAsync(BrunoRunOptions options);
}

public sealed class BrunoRunner : IBrunoRunner
{
    public Task<BrunoRunResult> RunAsync(BrunoRunOptions options);
}
```

---

## 5. ASP.NET Host Layer – `Teqniqly.BRUnit.Testing.AspNetCore`

```csharp
public abstract class BrunoAspNetHostBase<TEntryPoint>
    where TEntryPoint : class
{
    protected WebApplicationFactory<TEntryPoint> Factory { get; }
    protected IBrunoRunner BrunoRunner { get; }

    protected BrunoAspNetHostBase(WebApplicationFactory<TEntryPoint> factory,
                                  IBrunoRunner brunoRunner)
    {
        Factory = factory;
        BrunoRunner = brunoRunner;
    }

    protected virtual async Task<BrunoRunResult> RunBrunoAgainstTestServerAsync(
        BrunoRunOptions options, string baseUrlVariableName = "BASE_URL")
    {
        using var client = Factory.CreateClient();
        options.EnvironmentVariables[baseUrlVariableName] =
            client.BaseAddress!.ToString().TrimEnd('/');

        return await BrunoRunner.RunAsync(options);
    }
}
```

---

## 6. xUnit Package – `Teqniqly.BRUnit.Xunit`

```csharp
public abstract class BrunoXunitTestBase<TEntryPoint>
    : BrunoAspNetHostBase<TEntryPoint>, IClassFixture<WebApplicationFactory<TEntryPoint>>
    where TEntryPoint : class
{
    protected ITestOutputHelper Output { get; }

    protected BrunoXunitTestBase(WebApplicationFactory<TEntryPoint> factory,
                                 ITestOutputHelper output,
                                 IBrunoRunner? runner = null)
        : base(factory, runner ?? new BrunoRunner())
    {
        Output = output;
    }

    protected override async Task<BrunoRunResult> RunBrunoAgainstTestServerAsync(
        BrunoRunOptions options, string baseUrlVariableName = "BASE_URL")
    {
        var result = await base.RunBrunoAgainstTestServerAsync(options, baseUrlVariableName);
        Output.WriteLine(result.StandardOutput);
        if (!string.IsNullOrWhiteSpace(result.StandardError))
            Output.WriteLine(result.StandardError);

        return result;
    }
}
```

---

## 7. Usage Examples

### xUnit Usage

```csharp
public class ContractTests : BrunoXunitTestBase<Program>
{
    public ContractTests(WebApplicationFactory<Program> factory, ITestOutputHelper output)
        : base(factory, output) {}

    [Fact]
    public async Task ValidateContracts()
    {
        var result = await RunBrunoAgainstTestServerAsync(new BrunoRunOptions {
            WorkingDirectory = "./Bruno",
            Target = "upload-image.bru"
        });

        Assert.True(result.IsSuccess);
    }
}
```

### NUnit Example (no NUnit package needed)

```csharp
public class NUnitContractTests : BrunoAspNetHostBase<Program>
{
    public NUnitContractTests()
        : base(new WebApplicationFactory<Program>(), new BrunoRunner()) {}

    [Test]
    public async Task ValidateContracts()
    {
        var result = await RunBrunoAgainstTestServerAsync(new BrunoRunOptions {
            WorkingDirectory = "./Bruno",
            Target = "upload.bru"
        });

        Assert.That(result.IsSuccess, Is.True);
    }
}
```

---

## 8. Versioning

| Version | Feature                            |
| ------- | ---------------------------------- |
| `0.1.0` | Core runner + ASP.NET host         |
| `0.2.0` | Add xUnit integration              |
| `1.0.0` | Stable after real-world validation |

---

## 9. Summary

Teqniqly.BRUnit creates a contract-testing pipeline where .bru files can run inside
regular test frameworks against dynamic test servers.

Clean layered design enables:

```text
Core Runner  →  ASP.NET Host Base  →  xUnit/NUnit/MSTest Add-ons
```

This ensures maintainability, extensibility, clean separation of concerns,
and smooth test automation integration.

---
