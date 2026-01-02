using System.Collections.Immutable;
using Teqniqly.BRUnit.Testing;

// This console sample demonstrates how to use Teqniqly.BRUnit.Testing
// to run Bruno CLI contract tests against the JSONPlaceholder API.
//
// Prerequisites:
// 1. Bruno CLI must be installed: npm install -g @usebruno/cli
// 2. A Bruno collection file or folder must exist (see README.md in this folder)

var runner = new BrunoRunner(new ProcessFactory());
bool hasFailure = false;

// Example 1: Basic usage with a Bruno collection
var collectionPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "bruno-collection");
var basicOptions = new BrunoRunOptions
{
    Target = "requests",
    EnvironmentName = "Local", // Use Local environment for variable resolution
    WorkingDirectory = collectionPath,
};

Console.WriteLine("Running basic Bruno collection...");
try
{
    var result = await runner.RunAsync(basicOptions).ConfigureAwait(false);

    if (result.IsSuccess)
    {
        Console.WriteLine("✅ All contract tests passed!");
        Console.WriteLine(result.StandardOutput);
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
    Console.WriteLine("Install with: npm install -g @usebruno/cli");
    Environment.Exit(1);
}

// Example 2: Using environment name
var envOptions = new BrunoRunOptions
{
    Target = "requests",
    EnvironmentName = "production",
    WorkingDirectory = collectionPath,
};

Console.WriteLine("\nRunning with environment name...");
try
{
    var envResult = await runner.RunAsync(envOptions).ConfigureAwait(false);
    if (!envResult.IsSuccess)
    {
        Console.WriteLine($"Result: Failed (exit code: {envResult.ExitCode})");
        Console.WriteLine(envResult.StandardError);
        hasFailure = true;
    }
    else
    {
        Console.WriteLine("Result: Success");
    }
}
catch (TimeoutException ex)
{
    Console.WriteLine($"⏱️ Test execution timed out: {ex.Message}");
    hasFailure = true;
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"❌ Failed to start Bruno CLI: {ex.Message}");
    hasFailure = true;
}

// Example 3: Passing environment variables (process-level, not Bruno env vars)
// Bruno environment variables are defined in environments/*.bru files.
var envVarOptions = new BrunoRunOptions
{
    Target = "requests",
    EnvironmentName = "Local", // Still need Bruno environment for {{base_url}}
    EnvironmentVariables = ImmutableDictionary<string, string?>
        .Empty.Add("API_BASE_URL", "https://jsonplaceholder.typicode.com")
        .Add("API_TIMEOUT", "5000"),
    WorkingDirectory = collectionPath,
};

Console.WriteLine("\nRunning with environment variables...");
try
{
    var envVarResult = await runner.RunAsync(envVarOptions).ConfigureAwait(false);
    if (!envVarResult.IsSuccess)
    {
        Console.WriteLine($"Result: Failed (exit code: {envVarResult.ExitCode})");
        Console.WriteLine(envVarResult.StandardError);
        hasFailure = true;
    }
    else
    {
        Console.WriteLine("Result: Success");
    }
}
catch (TimeoutException ex)
{
    Console.WriteLine($"⏱️ Test execution timed out: {ex.Message}");
    hasFailure = true;
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"❌ Failed to start Bruno CLI: {ex.Message}");
    hasFailure = true;
}

// Example 4: Custom timeout
var timeoutOptions = new BrunoRunOptions
{
    Target = "requests",
    EnvironmentName = "Local",
    Timeout = TimeSpan.FromMinutes(5),
    WorkingDirectory = collectionPath,
};

Console.WriteLine("\nRunning with custom timeout...");
try
{
    var timeoutResult = await runner.RunAsync(timeoutOptions).ConfigureAwait(false);
    if (!timeoutResult.IsSuccess)
    {
        Console.WriteLine($"Result: Failed (exit code: {timeoutResult.ExitCode})");
        Console.WriteLine(timeoutResult.StandardError);
        hasFailure = true;
    }
    else
    {
        Console.WriteLine("Result: Success");
    }
}
catch (TimeoutException ex)
{
    Console.WriteLine($"⏱️ Test execution timed out: {ex.Message}");
    hasFailure = true;
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"❌ Failed to start Bruno CLI: {ex.Message}");
    hasFailure = true;
}

if (hasFailure)
{
    Console.WriteLine("\n❌ Console sample completed with failures!");
    Environment.Exit(1);
}

Console.WriteLine("\n✅ Console sample completed!");
