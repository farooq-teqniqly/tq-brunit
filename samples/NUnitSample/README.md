# NUnitSample

This sample demonstrates how to use `Teqniqly.BRUnit.Testing` in NUnit test projects to run Bruno CLI contract tests as part of your test suite.

## Prerequisites

1. **Bruno CLI**: Install globally via npm

   ```bash
   npm install -g @usebruno/cli
   ```

2. **Bruno Collection**: The sample uses the shared Bruno collection located at `samples/bruno-collection`. This collection is shared with other sample projects.

## Project Structure

- **`BrunoContractTests.cs`**: Contains example NUnit tests demonstrating various `BrunoRunner` usage scenarios
- **`BrunoCollectionFixture.cs`**: Helper class that provides the path to the Bruno collection

## Running the Tests

```bash
dotnet test
```

## Example Tests

The sample includes 7 example tests:

1. **`RunCollection_WithValidCollection_ReturnsSuccess`**: Basic test that runs a Bruno collection and verifies success
2. **`RunCollection_WithProductionEnvironment_ReturnsSuccess`**: Demonstrates using different Bruno environments
3. **`RunCollection_WithEnvironmentVariables_ReturnsSuccess`**: Shows how to pass process-level environment variables and verifies successful execution
4. **`RunCollection_CompletesWithinTimeout`**: Verifies that a successful collection execution completes within the configured timeout
5. **`RunCollection_WhenTimeoutExceeded_ThrowsTimeoutException`**: Demonstrates timeout enforcement by using a test helper that exceeds the timeout and verifying a `TimeoutException` is thrown
6. **`RunCollection_WhenCollectionFails_ReturnsFailure`**: Tests error handling when a collection fails
7. **`RunCollection_WithInvalidBrunoPath_ThrowsInvalidOperationException`**: Tests exception handling for invalid Bruno executable paths

## Key Features Demonstrated

- **NUnit Test Fixtures**: Using `[TestFixture]` and `[OneTimeSetUp]` to initialize test dependencies
- **Async/Await**: Proper async test patterns with NUnit
- **Assertions**: Using NUnit's constraint-based assertions (`Assert.That`) for readable test code
- **Error Handling**: Testing both success and failure scenarios
- **Environment Configuration**: Using different Bruno environments and process environment variables

## Integration with Your Test Suite

To integrate Bruno contract tests into your own NUnit test suite:

1. Add a project reference to `Teqniqly.BRUnit.Testing`
2. Create a helper class (like `BrunoCollectionFixture`) to manage your Bruno collection path
3. Write tests that use `BrunoRunner` to execute your Bruno collections
4. Assert on the results using `BrunoRunResult.IsSuccess`, `ExitCode`, `StandardOutput`, and `StandardError`

## Learn More

- [Main README](../../README.md) - Full library documentation
- [ConsoleSample](../ConsoleSample/README.md) - Console application example
- [XUnitSample](../XUnitSample/README.md) - XUnit test example
- [Technical Proposal](../../docs/proposals/BRUNIT_TESTING_CORE_TECHNICAL_PROPOSAL.md) - Architecture details

