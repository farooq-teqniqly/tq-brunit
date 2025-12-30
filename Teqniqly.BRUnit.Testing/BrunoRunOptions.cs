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
    /// Optional environment name to use (Bruno's --env flag).
    /// </summary>
    public string? EnvironmentName { get; init; }

    /// <summary>
    /// Environment variables to pass to the Bruno process.
    /// Keys are case-insensitive.
    /// </summary>
    public IDictionary<string, string?> EnvironmentVariables { get; init; } =
        new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Target .bru file or folder to execute. Required.
    /// </summary>
    public string Target { get; init; } = string.Empty;

    /// <summary>
    /// Maximum time to wait for Bruno execution. Defaults to 2 minutes.
    /// </summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Working directory for the Bruno process. Defaults to current directory.
    /// </summary>
    public string WorkingDirectory { get; init; } = Directory.GetCurrentDirectory();
}
