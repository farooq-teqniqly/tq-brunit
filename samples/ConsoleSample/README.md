# ConsoleSample

This sample demonstrates how to use `Teqniqly.BRUnit.Testing` to run Bruno CLI contract tests programmatically from .NET.

## Prerequisites

1. **Bruno CLI**: Install globally via npm

   ```bash
   npm install -g @usebruno/cli
   ```

2. **Bruno Collection**: Create a Bruno collection that tests the JSONPlaceholder API

## Setting Up a Bruno Collection

1. Create a folder named `bruno-collection` in this directory

2. Initialize a Bruno collection:

   ```bash
   cd bruno-collection
   bru init
   ```

3. Create a request file (e.g., `GetPost.bru`) that tests JSONPlaceholder:

   ```http
   GET {{base_url}}/posts/1
   ```

4. Create an environment file (e.g., `Local.bru`) with:

   ```json
   {
     "base_url": "https://jsonplaceholder.typicode.com"
   }
   ```

Alternatively, you can use an existing Bruno collection by updating the `Target` path in `Program.cs`.

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
