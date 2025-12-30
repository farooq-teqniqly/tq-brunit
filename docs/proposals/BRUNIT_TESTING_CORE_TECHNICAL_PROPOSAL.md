# Teqniqly.BRUnit.Testing – Technical Proposal

## 1. Summary

### What we will build in the first milestone

- **Core runner interface and implementation** (`IBrunoRunner`, `BrunoRunner`) that executes Bruno CLI (`bru`) as an external process
- **Configuration model** (`BrunoRunOptions`) for specifying Bruno execution parameters (executable path, working directory, target files, environment variables, timeout)
- **Result model** (`BrunoRunResult`) capturing exit code, stdout, stderr, and success status
- **Process execution abstraction** using `System.Diagnostics.Process` with proper async/await patterns
- **Cross-platform executable resolution** (handling `bru` vs `bru.exe` on Windows)
- **Comprehensive unit test suite** covering success paths, timeouts, missing executables, and error handling
- **NuGet package** targeting .NET 10 with zero dependencies on test frameworks

### Key decisions

- **Process-based execution**: Use `System.Diagnostics.Process` directly rather than shell wrappers for better control and cross-platform support
- **Immutable result types**: Use `sealed record` types with `init`-only properties for `BrunoRunOptions` and `BrunoRunResult` to ensure thread-safety, prevent mutation, and provide value equality
- **Interface-based design**: `IBrunoRunner` enables testability and future extensibility (e.g., mock runners for unit tests)
- **Dictionary for environment variables**: Use `IDictionary<string, string?>` with case-insensitive comparer to match Bruno CLI expectations
- **Default timeout of 2 minutes**: Reasonable default for API contract tests while allowing override
- **No test framework dependencies**: Keep core package completely agnostic; higher layers will add framework-specific integrations

### Risks (top 3)

1. **Bruno CLI availability**: If `bru` is not installed or not in PATH, tests will fail. Mitigation: Clear error messages and documentation.
2. **Process execution differences**: Windows vs Linux/macOS may have different executable resolution behavior. Mitigation: Explicit path handling and cross-platform testing.
3. **Output capture limits**: Very large stdout/stderr could cause memory issues. Mitigation: Reasonable limits and clear documentation.

---

## 2. Problem Statement

### Business problem

Teams using Bruno for API contract testing need to integrate these tests into their .NET integration test pipelines. Currently, Bruno tests run separately from .NET tests, making it difficult to:

- Run contract tests against ephemeral test servers (e.g., `WebApplicationFactory`)
- Include contract test results in standard `dotnet test` output
- Maintain a unified test execution pipeline
- Share environment configuration between .NET tests and Bruno tests

### User value

- **Unified test execution**: Run Bruno contract tests alongside .NET integration tests in a single pipeline
- **Dynamic server integration**: Execute Bruno tests against test servers with dynamically assigned ports/URLs
- **Framework flexibility**: Support any .NET test framework (xUnit, NUnit, MSTest) without coupling to specific frameworks
- **CI/CD integration**: Contract test failures appear as standard test failures in build pipelines

### Constraints

- Must work on Windows, Linux, and macOS
- Must not depend on any test framework (xUnit, NUnit, MSTest)
- Must support .NET 10
- Must handle Bruno CLI not being installed gracefully
- Must support both file and folder targets for Bruno collections
- Must allow environment variable injection for dynamic configuration

### Success criteria

- ✅ Can execute `bru run <target>` programmatically from .NET code
- ✅ Can capture exit code, stdout, and stderr from Bruno execution
- ✅ Can configure working directory, executable path, and environment variables
- ✅ Can handle timeouts gracefully
- ✅ Zero dependencies on test frameworks
- ✅ All public APIs are immutable and thread-safe
- ✅ Comprehensive unit test coverage (>90%)

---

## 3. Goals and Non-Goals

### Goals

1. **Framework-agnostic core**: Provide a clean, testable interface for executing Bruno CLI without any test framework dependencies
2. **Process execution**: Reliably execute `bru` CLI as an external process with full control over environment and working directory
3. **Result capture**: Capture exit codes, stdout, and stderr for test assertions
4. **Configuration flexibility**: Support all common Bruno CLI execution scenarios (files, folders, environments, custom paths)
5. **Cross-platform support**: Work consistently on Windows, Linux, and macOS
6. **Testability**: Design interfaces and abstractions that enable comprehensive unit testing

### Non-Goals (out of scope for this layer)

- ❌ Integration with specific test frameworks (xUnit, NUnit, MSTest) — handled by higher layers
- ❌ Integration with `WebApplicationFactory` — handled by `Teqniqly.BRUnit.Testing.AspNetCore`
- ❌ Bruno collection parsing or validation — Bruno CLI handles this
- ❌ Test result reporting/formatting — test frameworks handle this
- ❌ Bruno CLI installation or management — users must install Bruno separately
- ❌ Support for .NET Framework or .NET Standard — .NET 10 only

---

## 4. Solution Options

### Option A: Simple Process.Start wrapper

**Approach**: Minimal wrapper around `Process.Start` with basic configuration.

**Pros**:

- Simplest implementation
- Minimal code surface
- Fast to implement

**Cons**:

- Limited error handling
- Difficult to test (tightly coupled to `Process`)
- No timeout handling
- Poor cross-platform executable resolution

**Complexity**: Low  
**Risk**: Medium (process execution edge cases)  
**Time**: 1-2 days

### Option B: Robust process execution with abstractions (RECOMMENDED)

**Approach**: Full-featured implementation with:

- `IBrunoRunner` interface for testability
- Proper async/await with cancellation token support
- Timeout handling via `Process.WaitForExit(timeout)`
- Cross-platform executable resolution
- Comprehensive error handling and validation
- Immutable configuration and result types (using record types)

**Pros**:

- Highly testable (interface-based)
- Robust error handling
- Timeout support
- Cross-platform ready
- Extensible for future needs
- Thread-safe design

**Cons**:

- More code to maintain
- Slightly more complex

**Complexity**: Medium  
**Risk**: Low (well-understood patterns)  
**Time**: 3-4 days

### Option C: Shell command builder

**Approach**: Build shell commands (bash/cmd) and execute via shell.

**Pros**:

- Can leverage shell features (pipes, redirects)

**Cons**:

- Platform-specific (different shells)
- Security concerns (command injection)
- Harder to test
- Less control over process lifecycle

**Complexity**: High  
**Risk**: High (security, cross-platform issues)  
**Time**: 4-5 days

### Trade-offs Table

| Option                          | Pros                             | Cons                           | Complexity | Risk    | Time         |
| ------------------------------- | -------------------------------- | ------------------------------ | ---------- | ------- | ------------ |
| A: Simple wrapper               | Fast, minimal                    | Limited features, hard to test | Low        | Medium  | 1-2 days     |
| **B: Robust with abstractions** | **Testable, robust, extensible** | **More code**                  | **Medium** | **Low** | **3-4 days** |
| C: Shell builder                | Shell features                   | Security, platform issues      | High       | High    | 4-5 days     |

**Recommendation**: **Option B** — Provides the right balance of robustness, testability, and maintainability while keeping complexity manageable.

---

## 5. Proposed Solution (Option B)

### Architecture overview

The core package consists of three main components:

1. **Configuration Model** (`BrunoRunOptions`): Immutable configuration for Bruno execution
2. **Result Model** (`BrunoRunResult`): Immutable result containing exit code and output
3. **Runner** (`IBrunoRunner` interface + `BrunoRunner` implementation): Process execution logic

```text
┌─────────────────────────────────────────┐
│   Teqniqly.BRUnit.Testing               │
├─────────────────────────────────────────┤
│                                         │
│  ┌──────────────────┐                  │
│  │ BrunoRunOptions  │                  │
│  │ (immutable)    │                  │
│  └──────────────────┘                  │
│           │                             │
│           ▼                             │
│  ┌──────────────────┐                  │
│  │  IBrunoRunner    │                  │
│  │  (interface)     │                  │
│  └──────────────────┘                  │
│           │                             │
│           ▼                             │
│  ┌──────────────────┐                  │
│  │  BrunoRunner     │                  │
│  │  (implementation)│                  │
│  └──────────────────┘                  │
│           │                             │
│           ▼                             │
│  ┌──────────────────┐                  │
│  │ BrunoRunResult   │                  │
│  │ (immutable)      │                  │
│  └──────────────────┘                  │
│                                         │
└─────────────────────────────────────────┘
           │
           │ Uses
           ▼
┌─────────────────────────────────────────┐
│   System.Diagnostics.Process            │
│   (external process execution)          │
└─────────────────────────────────────────┘
```

### Contracts

#### `BrunoRunOptions`

```csharp
namespace Teqniqly.BRUnit.Testing;

/// <summary>
/// Configuration options for executing a Bruno CLI command.
/// </summary>
public sealed record BrunoRunOptions
{
    /// <summary>
    /// Path to the Bruno executable. Defaults to "bru" (assumes it's in PATH).
    /// </summary>
    public string BruExecutablePath { get; init; } = "bru";

    /// <summary>
    /// Working directory for the Bruno process. Defaults to current directory.
    /// </summary>
    public string WorkingDirectory { get; init; } = Directory.GetCurrentDirectory();

    /// <summary>
    /// Target .bru file or folder to execute. Required.
    /// </summary>
    public string Target { get; init; } = string.Empty;

    /// <summary>
    /// Optional environment name to use (Bruno's --env flag).
    /// </summary>
    public string? EnvironmentName { get; init; }

    /// <summary>
    /// Maximum time to wait for Bruno execution. Defaults to 2 minutes.
    /// </summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Environment variables to pass to the Bruno process.
    /// Keys are case-insensitive.
    /// </summary>
    public IDictionary<string, string?> EnvironmentVariables { get; init; }
        = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
}
```

#### `BrunoRunResult`

```csharp
namespace Teqniqly.BRUnit.Testing;

/// <summary>
/// Result of executing a Bruno CLI command.
/// </summary>
public sealed record BrunoRunResult
{
    /// <summary>
    /// Exit code from the Bruno process. 0 indicates success.
    /// </summary>
    public int ExitCode { get; init; }

    /// <summary>
    /// Standard output from the Bruno process.
    /// </summary>
    public string StandardOutput { get; init; } = string.Empty;

    /// <summary>
    /// Standard error output from the Bruno process.
    /// </summary>
    public string StandardError { get; init; } = string.Empty;

    /// <summary>
    /// Indicates whether the Bruno execution was successful (exit code 0).
    /// </summary>
    public bool IsSuccess => ExitCode == 0;
}
```

#### `IBrunoRunner`

```csharp
namespace Teqniqly.BRUnit.Testing;

/// <summary>
/// Interface for executing Bruno CLI commands.
/// </summary>
public interface IBrunoRunner
{
    /// <summary>
    /// Executes a Bruno CLI command with the specified options.
    /// </summary>
    /// <param name="options">Configuration options for the Bruno execution.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The result of the Bruno execution.</returns>
    /// <exception cref="ArgumentNullException">Thrown when options is null.</exception>
    /// <exception cref="ArgumentException">Thrown when options are invalid.</exception>
    /// <exception cref="TimeoutException">Thrown when execution exceeds the timeout.</exception>
    Task<BrunoRunResult> RunAsync(BrunoRunOptions options, CancellationToken cancellationToken = default);
}
```

#### `BrunoRunner`

```csharp
namespace Teqniqly.BRUnit.Testing;

/// <summary>
/// Default implementation of <see cref="IBrunoRunner"/> that executes Bruno CLI as an external process.
/// </summary>
public sealed class BrunoRunner : IBrunoRunner
{
    /// <inheritdoc />
    public Task<BrunoRunResult> RunAsync(BrunoRunOptions options, CancellationToken cancellationToken = default)
    {
        // Implementation details:
        // 1. Validate options (null check, required fields)
        // 2. Resolve executable path (handle .exe on Windows)
        // 3. Build process start info (working directory, env vars, arguments)
        // 4. Start process with redirected stdout/stderr
        // 5. Wait for completion with timeout support
        // 6. Capture output streams
        // 7. Return BrunoRunResult
    }
}
```

### Data model

No persistent data model required for this layer. All data is:

- **Input**: `BrunoRunOptions` (in-memory configuration)
- **Output**: `BrunoRunResult` (in-memory result)

### Security

- **No authentication/authorization**: This layer executes local CLI tools only
- **Input validation**: Validate all `BrunoRunOptions` properties to prevent command injection
- **Path validation**: Ensure `WorkingDirectory` and `BruExecutablePath` are safe (no path traversal)
- **Environment variable sanitization**: No special handling needed; .NET `ProcessStartInfo` handles this safely

### Error handling

#### Exception types

1. **`ArgumentNullException`**: When `options` is null
2. **`ArgumentException`**: When required options are invalid (empty `Target`, invalid `WorkingDirectory`, etc.)
3. **`TimeoutException`**: When Bruno execution exceeds `Timeout`
4. **`FileNotFoundException`**: When `BruExecutablePath` cannot be found (if explicit path provided)
5. **`InvalidOperationException`**: When process cannot be started (rare system-level issues)

#### Error scenarios and handling

| Scenario                   | Handling                                                                                                                           |
| -------------------------- | ---------------------------------------------------------------------------------------------------------------------------------- |
| Bruno executable not found | Return `BrunoRunResult` with non-zero exit code (Bruno CLI handles this) OR throw `FileNotFoundException` if explicit path invalid |
| Timeout exceeded           | Throw `TimeoutException` with clear message                                                                                        |
| Invalid working directory  | Throw `ArgumentException` during validation                                                                                        |
| Process start failure      | Throw `InvalidOperationException` with inner exception details                                                                     |
| Bruno execution fails      | Return `BrunoRunResult` with non-zero `ExitCode` and error details in `StandardError`                                              |

**Design decision**: Prefer returning `BrunoRunResult` with error details over throwing exceptions for expected failures (Bruno CLI errors). Only throw for unexpected system-level issues.

---

## 6. Testing Strategy

### Unit test plan

**Test project**: `Teqniqly.BRUnit.Testing.Tests`

**Framework**: xUnit (per Teqniqly stack conventions)  
**Mocking**: NSubstitute (per Teqniqly stack conventions)  
**Assertions**: Built-in xUnit assertions (per Teqniqly stack conventions)

#### Test categories

1. **`BrunoRunOptions` validation tests**

   - Default values are correct
   - Immutability (cannot modify after initialization)
   - Value equality works correctly (record feature)

2. **`BrunoRunResult` tests**

   - `IsSuccess` returns true for exit code 0
   - `IsSuccess` returns false for non-zero exit codes
   - Immutability
   - Value equality works correctly (record feature)

3. **`BrunoRunner.RunAsync` success path tests**

   - Executes Bruno CLI successfully
   - Captures stdout correctly
   - Captures stderr correctly
   - Returns exit code 0 for success
   - Sets working directory correctly
   - Passes environment variables correctly
   - Handles environment name parameter

4. **`BrunoRunner.RunAsync` error path tests**

   - Returns non-zero exit code when Bruno fails
   - Captures error output in stderr
   - Handles missing executable gracefully
   - Throws `TimeoutException` when timeout exceeded
   - Throws `ArgumentNullException` for null options
   - Throws `ArgumentException` for invalid options

5. **Cross-platform tests**

   - Executable resolution on Windows (`.exe` handling)
   - Executable resolution on Linux/macOS
   - Path separator handling

6. **Edge cases**
   - Very long stdout/stderr (memory considerations)
   - Empty target
   - Special characters in paths
   - Concurrent executions (thread-safety)

#### Test structure

```csharp
// Example test structure
public class BrunoRunnerTests
{
    [Fact]
    public async Task RunAsync_WithValidOptions_ReturnsSuccessResult()
    {
        // Arrange
        var options = new BrunoRunOptions
        {
            Target = "test.bru",
            WorkingDirectory = "./TestData"
        };
        var runner = new BrunoRunner();

        // Act
        var result = await runner.RunAsync(options);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.ExitCode);
        // Records provide value equality - can compare options/results if needed
    }

    [Fact]
    public async Task RunAsync_WithTimeout_ThrowsTimeoutException()
    {
        // Arrange
        var options = new BrunoRunOptions
        {
            Target = "slow-test.bru",
            Timeout = TimeSpan.FromMilliseconds(100)
        };
        var runner = new BrunoRunner();

        // Act & Assert
        await Assert.ThrowsAsync<TimeoutException>(
            () => runner.RunAsync(options));
    }
}
```

### Integration test plan

**Note**: Integration tests require Bruno CLI to be installed. These should be marked as conditional or documented clearly.

1. **Golden path integration test**

   - Execute a real `.bru` file against a mock HTTP server
   - Verify success result
   - Verify output capture

2. **Failure path integration test**

   - Execute a `.bru` file that fails
   - Verify non-zero exit code
   - Verify error output

3. **Environment variable integration test**
   - Pass environment variables to Bruno
   - Verify Bruno receives them correctly

### Test coverage target

- **Unit tests**: >90% code coverage
- **Integration tests**: Cover all public API scenarios

---

## 7. Delivery Plan

### Phase 0: Scaffolding (Day 1)

- [ ] Create `Teqniqly.BRUnit.Testing` project with .NET 10 target
- [ ] Configure project properties (nullable, implicit usings, etc.)
- [ ] Set up `Teqniqly.BRUnit.Testing.Tests` project with xUnit
- [ ] Add NSubstitute to test project
- [ ] Configure solution structure

### Phase 1: Core models (Day 1-2)

- [ ] Implement `BrunoRunOptions` with all properties
- [ ] Implement `BrunoRunResult` with all properties
- [ ] Add XML documentation comments
- [ ] Write unit tests for models (validation, immutability)

### Phase 2: Runner interface and implementation (Day 2-3)

- [ ] Define `IBrunoRunner` interface
- [ ] Implement `BrunoRunner` with process execution
- [ ] Add executable path resolution (Windows `.exe` handling)
- [ ] Add timeout support
- [ ] Add environment variable passing
- [ ] Add error handling and validation
- [ ] Write comprehensive unit tests

### Phase 3: Cross-platform testing and refinement (Day 3-4)

- [ ] Test on Windows
- [ ] Test on Linux (if available)
- [ ] Test on macOS (if available)
- [ ] Fix cross-platform issues
- [ ] Add integration tests (conditional)
- [ ] Performance testing (concurrent executions)

### Phase 4: Documentation and packaging (Day 4)

- [ ] Add XML documentation to all public APIs
- [ ] Create README with usage examples
- [ ] Configure NuGet package metadata
- [ ] Version as `0.1.0` (per spec)
- [ ] Publish to local NuGet feed for testing

### Rollout notes

- **No feature flags needed**: This is a new library with no existing users
- **No migration needed**: Greenfield implementation
- **Backwards compatibility**: N/A for initial release
- **Breaking changes**: None expected in `0.1.0` (first release)

---

## 8. Observability

### Logging

**Decision**: No logging in core package. Reasons:

- Keep package lightweight and framework-agnostic
- Higher layers (ASP.NET, xUnit) can add logging/observability
- Users can capture stdout/stderr from `BrunoRunResult` for their own logging

**Future consideration**: If logging is needed, use `Microsoft.Extensions.Logging.Abstractions` (interface-only package) to maintain framework agnosticism.

### Metrics

**Not applicable** for this layer. Higher layers may add metrics for:

- Bruno execution duration
- Success/failure rates
- Timeout occurrences

### Traces

**Not applicable** for this layer. Process execution is synchronous from the caller's perspective.

### Debugging support

- **Clear error messages**: All exceptions include descriptive messages
- **Result details**: `BrunoRunResult` contains full stdout/stderr for debugging
- **Timeout information**: `TimeoutException` includes timeout duration and elapsed time

---

## 9. Risks & Mitigations

| Risk                                         | Impact                               | Mitigation                                                                                           | Owner    |
| -------------------------------------------- | ------------------------------------ | ---------------------------------------------------------------------------------------------------- | -------- |
| Bruno CLI not installed                      | High — tests will fail               | Clear error messages in `BrunoRunResult.StandardError`; document installation requirements in README | Dev team |
| Cross-platform executable resolution issues  | Medium — Windows vs Unix differences | Explicit `.exe` handling for Windows; test on multiple platforms; use `ProcessStartInfo` correctly   | Dev team |
| Process execution timeouts in CI             | Medium — flaky tests                 | Default 2-minute timeout is reasonable; allow override; document timeout tuning                      | Dev team |
| Large output causing memory issues           | Low — rare but possible              | Document limits; consider streaming for future versions if needed                                    | Dev team |
| Process start failures on restricted systems | Low — edge case                      | Clear error messages; handle `InvalidOperationException` gracefully                                  | Dev team |
| Concurrent execution issues                  | Low — unlikely in test scenarios     | Ensure thread-safety in `BrunoRunner`; test concurrent executions                                    | Dev team |

---

## 10. Open Questions for Review

### Q1: Should we support cancellation tokens?

**Question**: Should `IBrunoRunner.RunAsync` accept a `CancellationToken` to allow cancellation of long-running Bruno executions?

**Recommendation**: **Yes**. Add `CancellationToken cancellationToken = default` parameter. This is standard async pattern and allows test timeouts and user cancellation.

**Status**: ✅ Included in proposal

### Q2: How should we handle very large stdout/stderr?

**Question**: Should we implement streaming or size limits for output capture?

**Recommendation**: **No limits initially**. Bruno CLI output is typically small. If issues arise, we can add limits in a future version. Document this as a known limitation.

**Status**: ✅ Documented in risks

### Q3: Should we validate that Bruno executable exists before execution?

**Question**: Should we check if `bru` exists in PATH before attempting execution?

**Recommendation**: **No explicit check**. Let the process execution fail naturally and return a meaningful error in `BrunoRunResult`. Explicit checks are platform-specific and may have false negatives (executable exists but not executable, etc.). Bruno CLI will return a clear error if not found.

**Status**: ✅ Decision made

### Q4: Should we support custom Bruno CLI arguments?

**Question**: Should `BrunoRunOptions` support passing arbitrary CLI arguments to Bruno?

**Recommendation**: **Not in v0.1.0**. The spec defines specific options (target, environment). If needed, we can add an `AdditionalArguments` property in a future version. Keep API surface minimal initially.

**Status**: ✅ Out of scope for first milestone

### Q5: Should we provide a factory or builder pattern for `BrunoRunOptions`?

**Question**: Should we add a fluent builder for creating options?

**Recommendation**: **No**. The `init`-only properties with object initializer syntax are sufficient and idiomatic C#. Keep it simple.

**Status**: ✅ Decision made

---

## 11. References

- **Specification**: [`docs/Teqniqly.BRUnit-Spec.md`](../Teqniqly.BRUnit-Spec.md) — Primary source of truth for API design
- **Teqniqly Stack Rules**: [`.cursor/rules/TEQNIQLY_STACK.cursorrules`](../../.cursor/rules/TEQNIQLY_STACK.cursorrules) — Development conventions
- **Bruno CLI Documentation**: [https://www.usebruno.com/](https://www.usebruno.com/) — External reference for Bruno CLI behavior
- **.NET Process Documentation**: [https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.process](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.process) — Process execution API

---

## Appendix A: Project Structure

After implementation, the project structure will be:

```text
Teqniqly.BRUnit.Testing/
├── Teqniqly.BRUnit.Testing.csproj
├── BrunoRunOptions.cs
├── BrunoRunResult.cs
├── IBrunoRunner.cs
└── BrunoRunner.cs

Teqniqly.BRUnit.Testing.Tests/
├── Teqniqly.BRUnit.Testing.Tests.csproj
├── BrunoRunOptionsTests.cs
├── BrunoRunResultTests.cs
└── BrunoRunnerTests.cs
```

---

## Appendix B: Implementation Notes

### Process execution pattern

```csharp
// Pseudo-code for BrunoRunner.RunAsync implementation
public async Task<BrunoRunResult> RunAsync(BrunoRunOptions options, CancellationToken cancellationToken = default)
{
    // 1. Validate options
    if (options == null) throw new ArgumentNullException(nameof(options));
    if (string.IsNullOrWhiteSpace(options.Target)) throw new ArgumentException(...);

    // 2. Resolve executable path
    var executablePath = ResolveExecutablePath(options.BruExecutablePath);

    // 3. Build process start info
    var startInfo = new ProcessStartInfo
    {
        FileName = executablePath,
        Arguments = BuildArguments(options),
        WorkingDirectory = options.WorkingDirectory,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
    };

    // 4. Add environment variables
    foreach (var (key, value) in options.EnvironmentVariables)
    {
        startInfo.Environment[key] = value;
    }

    // 5. Start process and capture output
    using var process = Process.Start(startInfo);
    if (process == null) throw new InvalidOperationException(...);

    // 6. Wait with timeout
    var outputTask = process.StandardOutput.ReadToEndAsync();
    var errorTask = process.StandardError.ReadToEndAsync();
    var completed = await Task.WhenAny(
        Task.Run(async () => { await process.WaitForExitAsync(cancellationToken); }, cancellationToken),
        Task.Delay(options.Timeout, cancellationToken)
    );

    if (!process.HasExited)
    {
        process.Kill();
        throw new TimeoutException(...);
    }

    var output = await outputTask;
    var error = await errorTask;

    return new BrunoRunResult
    {
        ExitCode = process.ExitCode,
        StandardOutput = output,
        StandardError = error
    };
}
```

### Executable path resolution

```csharp
private static string ResolveExecutablePath(string path)
{
    // If path contains directory separators, use as-is
    if (Path.IsPathRooted(path) || path.Contains(Path.DirectorySeparatorChar))
    {
        return path;
    }

    // On Windows, try .exe extension
    if (OperatingSystem.IsWindows() && !path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
    {
        var exePath = path + ".exe";
        // Could check if exists, but let Process.Start handle it
        return exePath;
    }

    return path;
}
```

---

**Document Status**: ✅ Ready for review  
**Target Milestone**: v0.1.0 (Core runner + ASP.NET host per spec)  
**Estimated Effort**: 3-4 days  
**Next Steps**: Review → Approval → Implementation
