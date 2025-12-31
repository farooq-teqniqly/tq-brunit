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
    /// <exception cref="ArgumentException">Thrown when options are invalid (e.g., BruExecutablePath or Target is null, empty, or whitespace).</exception>
    /// <exception cref="TimeoutException">Thrown when execution exceeds the timeout specified in <paramref name="options"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the process cannot be started.</exception>
    public Task<BrunoRunResult> RunAsync(
        BrunoRunOptions options,
        CancellationToken cancellationToken = default
    );
}
