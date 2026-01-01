﻿﻿﻿using System.Collections.Immutable;
using Teqniqly.BRUnit.Testing;

// This console sample demonstrates how to use Teqniqly.BRUnit.Testing
// to run Bruno CLI contract tests against the JSONPlaceholder API.
//
// Prerequisites:
// 1. Bruno CLI must be installed: npm install -g @usebruno/cli
// 2. A Bruno collection file or folder must exist (see README.md in this folder)

var runner = new BrunoRunner(new ProcessFactory());

// Example 1: Basic usage with a Bruno collection
var collectionPath = Path.Combine(Directory.GetCurrentDirectory(), "bruno-collection");
var basicOptions = new BrunoRunOptions
{
    Target = "requests", // Run requests folder
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
    Target = "requests", // Run requests folder
    EnvironmentName = "production", // Bruno environment name
    WorkingDirectory = collectionPath,
};

Console.WriteLine("\nRunning with environment name...");
try
{
    var envResult = await runner.RunAsync(envOptions).ConfigureAwait(false);
// Example 3: Passing environment variables (process-level, not Bruno env vars)
// Note: These are process environment variables, not Bruno environment variables.
// Bruno environment variables are defined in environments/*.bru files.
var envVarOptions = new BrunoRunOptions
{
    Target = "requests", // Run requests folder
    EnvironmentName = "Local", // Still need Bruno environment for {{base_url}}
    EnvironmentVariables = ImmutableDictionary<string, string?>
        .Empty
        .Add("API_BASE_URL", "https://jsonplaceholder.typicode.com")
        .Add("API_TIMEOUT", "5000"),
    WorkingDirectory = collectionPath,
};

Console.WriteLine("\nRunning with environment variables...");
try
{
    var envVarResult = await runner.RunAsync(envVarOptions).ConfigureAwait(false);
    Console.WriteLine($"Result: {(envVarResult.IsSuccess ? "Success" : "Failed")}");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Example 3 failed: {ex.Message}");
}
        .Add("API_TIMEOUT", "5000"),
    WorkingDirectory = collectionPath,
};

Console.WriteLine("\nRunning with environment variables...");
var envVarResult = await runner.RunAsync(envVarOptions).ConfigureAwait(false);
Console.WriteLine($"Result: {(envVarResult.IsSuccess ? "Success" : "Failed")}");

// Example 4: Custom timeout
var timeoutOptions = new BrunoRunOptions
{
    Target = "requests", // Run requests folder
    EnvironmentName = "Local", // Need environment for variable resolution
    Timeout = TimeSpan.FromMinutes(5),
    WorkingDirectory = collectionPath,
};

Console.WriteLine("\nRunning with custom timeout...");
try
{
    var timeoutResult = await runner.RunAsync(timeoutOptions).ConfigureAwait(false);
    Console.WriteLine($"Result: {(timeoutResult.IsSuccess ? "Success" : "Failed")}");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Example 4 failed: {ex.Message}");
}

Console.WriteLine("\n✅ Console sample completed!");
