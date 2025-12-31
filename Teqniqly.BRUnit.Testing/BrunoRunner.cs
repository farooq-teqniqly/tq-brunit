namespace Teqniqly.BRUnit.Testing;

/// <summary>
/// Default implementation of <see cref="IBrunoRunner"/> that executes Bruno CLI as an external process.
/// </summary>
public sealed class BrunoRunner : IBrunoRunner
{
    /// <inheritdoc />
    public Task<BrunoRunResult> RunAsync(
        BrunoRunOptions options,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(options);

        throw new NotImplementedException();
    }
}
