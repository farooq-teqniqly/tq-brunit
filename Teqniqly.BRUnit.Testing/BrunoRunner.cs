using System.Diagnostics;

namespace Teqniqly.BRUnit.Testing;

/// <summary>
/// Default implementation of <see cref="IBrunoRunner"/> that executes Bruno CLI as an external process.
/// </summary>
public sealed class BrunoRunner : IBrunoRunner
{
    private readonly IProcessFactory _processFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="BrunoRunner"/> class.
    /// </summary>
    /// <param name="processFactory">The process factory to use for creating processes.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="processFactory"/> is null.</exception>
    public BrunoRunner(IProcessFactory processFactory)
    {
        ArgumentNullException.ThrowIfNull(processFactory);
        _processFactory = processFactory;
    }

    /// <inheritdoc />
    public async Task<BrunoRunResult> RunAsync(
        BrunoRunOptions options,
        CancellationToken cancellationToken = default
    )
    {
        ValidateOptions(options);

        var startInfo = CreateProcessStartInfo(options);
        var process = _processFactory.Start(startInfo);

        return await ExecuteProcessAndCaptureOutput(process, cancellationToken)
            .ConfigureAwait(false);
    }

    private static ProcessStartInfo CreateProcessStartInfo(BrunoRunOptions options)
    {
        return new ProcessStartInfo
        {
            FileName = options.BruExecutablePath,
            Arguments = options.Target,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
    }

    private static async Task<BrunoRunResult> ExecuteProcessAndCaptureOutput(
        Process process,
        CancellationToken cancellationToken
    )
    {
        using (process)
        {
            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

            var output = await outputTask.ConfigureAwait(false);
            var error = await errorTask.ConfigureAwait(false);

            return new BrunoRunResult
            {
                ExitCode = process.ExitCode,
                StandardOutput = output,
                StandardError = error,
            };
        }
    }

    private static void ValidateOptions(BrunoRunOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.BruExecutablePath))
        {
            throw new ArgumentException(
                "BruExecutablePath cannot be null or empty.",
                nameof(options)
            );
        }

        if (string.IsNullOrWhiteSpace(options.Target))
        {
            throw new ArgumentException("Target cannot be null or empty.", nameof(options));
        }
    }
}
