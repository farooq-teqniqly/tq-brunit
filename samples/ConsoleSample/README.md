# ConsoleSample

This sample demonstrates how to use `Teqniqly.BRUnit.Testing` to run Bruno CLI contract tests programmatically from .NET.

## Prerequisites

1. **Bruno CLI**: Install globally via npm

   ```bash
   npm install -g @usebruno/cli
   ```

2. **Bruno Collection**: The sample uses the shared Bruno collection located at `samples/bruno-collection`. This collection is shared with other sample projects.

## Running the Sample

```bash
dotnet run
```

The sample demonstrates:

- Basic Bruno collection execution
- Using environment names
- Passing environment variables
- Custom timeout configuration

## Example Output

```text
Running basic Bruno collection...
✅ All contract tests passed!
[Bruno output here]

Running with environment name...
Result: Success

Running with environment variables...
Result: Success

Running with custom timeout...
Result: Success

✅ Console sample completed!
```

## Learn More

- [Main README](../../README.md) - Full documentation
- [Technical Proposal](../../docs/proposals/BRUNIT_TESTING_CORE_TECHNICAL_PROPOSAL.md) - Architecture details
- [JSONPlaceholder API](https://jsonplaceholder.typicode.com) - Test API used in examples
- [Bruno CLI Documentation](https://docs.usebruno.com/bru-cli) - Bruno CLI reference
