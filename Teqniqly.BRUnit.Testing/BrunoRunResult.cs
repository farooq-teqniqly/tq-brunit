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
    /// Standard error output from the Bruno process.
    /// </summary>
    public string StandardError { get; init; } = string.Empty;

    /// <summary>
    /// Standard output from the Bruno process.
    /// </summary>
    public string StandardOutput { get; init; } = string.Empty;
}
