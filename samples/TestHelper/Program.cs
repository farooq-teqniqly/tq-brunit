// Simple test helper that mimics Bruno CLI behavior for timeout testing
// Accepts arguments like Bruno CLI: "run <sleep-seconds>"
// Usage: TestHelper.exe run <sleep-seconds> [exit-code]
// Or: TestHelper.exe <sleep-seconds> [exit-code] (for direct invocation)

int sleepSeconds;
var argIndex = 0;

// Skip "run" if present (Bruno CLI format)
if (args.Length > 0 && args[0].Equals("run", StringComparison.OrdinalIgnoreCase))
{
    argIndex = 1;
}

if (args.Length <= argIndex)
{
    await Console.Error.WriteLineAsync("Usage: TestHelper.exe [run] <sleep-seconds> [exit-code]");
    Environment.Exit(1);
}
if (!int.TryParse(args[argIndex], out sleepSeconds) || sleepSeconds < 0)
{
    await Console.Error.WriteLineAsync($"Invalid sleep duration: {args[argIndex]}");
    Environment.Exit(1);
    return;
}

var exitCode = 0;
var exitCodeIndex = argIndex + 1;
if (args.Length > exitCodeIndex && int.TryParse(args[exitCodeIndex], out var parsedExitCode))
{
    exitCode = parsedExitCode;
}

// Sleep for the specified duration
Thread.Sleep(TimeSpan.FromSeconds(sleepSeconds));

// Write some output to simulate Bruno behavior
await Console.Out.WriteLineAsync("Test helper completed");
await Console.Error.WriteLineAsync("Test helper stderr output");

Environment.Exit(exitCode);
