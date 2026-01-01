# Teqniqly.BRUnit.Testing

A .NET library for executing [Bruno CLI](https://docs.usebruno.com/bru-cli) contract tests programmatically from .NET code. This library enables running Bruno API contract tests alongside .NET integration tests in a unified test pipeline.

## Overview

**Teqniqly.BRUnit.Testing** provides a framework-agnostic core runner for executing Bruno CLI (`bru`) commands as external processes. It bridges application-level integration testing and contract testing by allowing `.bru` files to run against ephemeral test servers.

### Key Features

- **Framework-agnostic**: No dependencies on test frameworks (xUnit, NUnit, MSTest)
- **Cross-platform**: Works on Windows, Linux, and macOS with automatic executable resolution
- **Async/await support**: Fully asynchronous API with cancellation token support
- **Timeout handling**: Configurable timeouts with automatic process cleanup
- **Environment support**: Pass environment variables and Bruno environment names
- **Immutable types**: Thread-safe, immutable configuration and result models
- **Zero test framework dependencies**: Core package is completely agnostic

## Installation

Install the package from NuGet:

```bash
dotnet add package Teqniqly.BRUnit.Testing
```

Or via Package Manager:

```powershell
Install-Package Teqniqly.BRUnit.Testing
```

### .NET Version Requirements

This package targets .NET 10 and is tested against .NET 8, 9, and 10 in CI.

## Bruno CLI Requirements

**Teqniqly.BRUnit.Testing** requires the Bruno CLI to be installed separately. The library executes `bru` as an external process.

### Installing Bruno CLI

Install Bruno CLI globally via npm:

```bash
npm install -g @usebruno/cli
```

Verify installation:

```bash
bru --version
```

### PATH Requirements

Ensure `bru` (or `bru.exe` on Windows) is available in your system PATH. The library will automatically resolve the executable path on Windows by adding the `.exe` extension when needed.

For more information, see the [Bruno CLI documentation](https://docs.usebruno.com/bru-cli).

## Quick Start

### Basic Usage

The following example demonstrates running a Bruno collection against the [JSONPlaceholder](https://jsonplaceholder.typicode.com) API:

```csharp
using Teqniqly.BRUnit.Testing;

var runner = new BrunoRunner(new ProcessFactory());
var options = new BrunoRunOptions
{
    Target = "api-tests.bru" // Your Bruno collection file or folder
};

var result = await runner.RunAsync(options);

if (result.IsSuccess)
{
    Console.WriteLine("All tests passed!");
    Console.WriteLine(result.StandardOutput);
}
else
{
    Console.WriteLine($"Tests failed with exit code: {result.ExitCode}");
    Console.WriteLine(result.StandardError);
}
```

### Using Environment Names

Bruno supports multiple environments (e.g., `production`, `staging`, `development`). Specify an environment using the `EnvironmentName` property:

```csharp
var options = new BrunoRunOptions
{
    Target = "api-tests.bru",
    EnvironmentName = "production"
};

var result = await runner.RunAsync(options);
```

### Passing Environment Variables

Pass environment variables to the Bruno process for dynamic configuration:

````csharp
using System.Collections.Immutable;

var envVars = new Dictionary<string, string?>
{
    ["API_BASE_URL"] = "https://jsonplaceholder.typicode.com",
    ["API_TIMEOUT"] = "5000"
}.ToImmutableDictionary();

var options = new BrunoRunOptions
{
    Target = "api-tests.bru",
    EnvironmentVariables = envVars
};

var result = await runner.RunAsync(options);
### Custom Timeout

Configure a custom timeout for long-running tests:

```csharp
var options = new BrunoRunOptions
{
    Target = "slow-api-tests.bru",
    Timeout = TimeSpan.FromMinutes(5)
};

var result = await runner.RunAsync(options);
````

### Complete Example

Here's a complete example that tests against JSONPlaceholder:

```csharp
using System.Collections.Immutable;
using Teqniqly.BRUnit.Testing;

var runner = new BrunoRunner(new ProcessFactory());

var options = new BrunoRunOptions
{
    Target = "./bruno-collection",
    EnvironmentName = "test",
    EnvironmentVariables = ImmutableDictionary<string, string?>
        .Empty
        .Add("API_BASE_URL", "https://jsonplaceholder.typicode.com"),
    Timeout = TimeSpan.FromMinutes(2),
    WorkingDirectory = "./tests"
};

try
{
    var result = await runner.RunAsync(options);

    if (result.IsSuccess)
    {
        Console.WriteLine("✅ All contract tests passed!");
    }
    else
    {
        Console.WriteLine($"❌ Tests failed (exit code: {result.ExitCode})");
        Console.WriteLine(result.StandardError);
        Environment.Exit(1);
    }
}
catch (TimeoutException ex)
{
    Console.WriteLine($"⏱️ Test execution timed out: {ex.Message}");
    Environment.Exit(1);
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"❌ Failed to start Bruno CLI: {ex.Message}");
    Console.WriteLine("Make sure Bruno CLI is installed and available in PATH.");
    Environment.Exit(1);
}
```

**Note:** The examples above use [JSONPlaceholder](https://jsonplaceholder.typicode.com) - a free fake REST API perfect for testing. You can create a Bruno collection that tests endpoints like `GET /posts/1`, `POST /posts`, etc.

## API Reference

The library provides the following main types:

- **`IBrunoRunner`**: Interface for executing Bruno CLI commands
- **`BrunoRunner`**: Default implementation that executes Bruno CLI as an external process
- **`BrunoRunOptions`**: Immutable configuration for Bruno execution
- **`BrunoRunResult`**: Immutable result containing exit code, stdout, and stderr
- **`IProcessFactory`**: Factory interface for creating process instances (for testability)
- **`ProcessFactory`**: Default implementation using `System.Diagnostics.Process`

For detailed API documentation, see the XML documentation comments in the code or generate documentation using tools like [DocFX](https://dotnet.github.io/docfx/) or [Sandcastle](https://github.com/EWSoftware/SHFB).

## Documentation

For detailed technical information, architecture decisions, and design rationale, see the [Technical Proposal](docs/proposals/BRUNIT_TESTING_CORE_TECHNICAL_PROPOSAL.md).

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
