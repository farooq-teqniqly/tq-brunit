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

            try
            {
                await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // When cancellation is requested during process execution, we need to ensure proper cleanup:
                // 1. Kill the process (and its entire process tree) to prevent it from continuing to run
                // 2. Wait for the process to actually terminate (using CancellationToken.None to avoid
                //    another cancellation exception)
                // 3. Gather any output/error that was captured before cancellation occurred
                // 4. Rethrow the OperationCanceledException to preserve cancellation semantics for the caller
                //
                // Note: The Kill operation is wrapped in a try-catch because the process may have already
                // exited by the time we attempt to kill it, which would throw an exception. We ignore such
                // exceptions as they don't affect the cleanup process.
                try
                {
                    process.Kill(entireProcessTree: true);
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception)
#pragma warning restore CA1031
                {
                    // Ignore errors when killing the process (e.g., already exited)
                }

                // Wait for process to terminate without cancellation token
                await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);

                // Gather output/error that was captured before cancellation
                await outputTask.ConfigureAwait(false);
                await errorTask.ConfigureAwait(false);

                // Rethrow to preserve cancellation
                throw;
            }

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
