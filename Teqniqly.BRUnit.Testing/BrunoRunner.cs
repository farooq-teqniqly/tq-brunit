using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

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
    /// <exception cref="TimeoutException">Thrown when execution exceeds the timeout specified in <paramref name="options"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the process cannot be started.</exception>
    public async Task<BrunoRunResult> RunAsync(
        BrunoRunOptions options,
        CancellationToken cancellationToken = default
    )
    {
        ValidateOptions(options);

        var startInfo = CreateProcessStartInfo(options);
        var process = _processFactory.Start(startInfo);

        return await ExecuteProcessAndCaptureOutput(process, options.Timeout, cancellationToken)
            .ConfigureAwait(false);
    }

    [ExcludeFromCodeCoverage(
        Justification = "Helper method that suppresses cancellation exceptions to preserve original exception context. Testing cancellation scenarios reliably is difficult."
    )]
    private static async Task AwaitIgnoringCancellationAsync(Task<string> task)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Suppress cancellation to preserve original exception
        }
    }

    private static ProcessStartInfo CreateProcessStartInfo(BrunoRunOptions options)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = ResolveExecutablePath(options.BruExecutablePath),
            WorkingDirectory = options.WorkingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        // Build arguments using ArgumentList for proper cross-platform escaping
        startInfo.ArgumentList.Add("run");

        if (!string.IsNullOrWhiteSpace(options.EnvironmentName))
        {
            startInfo.ArgumentList.Add("--env");
            startInfo.ArgumentList.Add(options.EnvironmentName);
        }

        startInfo.ArgumentList.Add(options.Target);

        SetEnvironmentVariables(startInfo, options.EnvironmentVariables);

        return startInfo;
    }

    private static async Task<BrunoRunResult> ExecuteProcessAndCaptureOutput(
        Process process,
        TimeSpan timeout,
        CancellationToken cancellationToken
    )
    {
        using (process)
        {
            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

            var waitTask = process.WaitForExitAsync(cancellationToken);
            var timeoutTask = Task.Delay(timeout, cancellationToken);
            var completedTask = await Task.WhenAny(waitTask, timeoutTask).ConfigureAwait(false);

            if (completedTask == timeoutTask && !process.HasExited)
            {
                await HandleProcessCleanupAsync(process, outputTask, errorTask)
                    .ConfigureAwait(false);
                throw new TimeoutException(
                    $"Bruno execution exceeded timeout of {timeout.TotalSeconds} seconds."
                );
            }

            try
            {
                await waitTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // When cancellation is requested during process execution, we need to ensure proper cleanup:
                // 1. Kill the process (and its entire process tree) to prevent it from continuing to run
                // 2. Wait for the process to actually terminate (using CancellationToken.None to avoid
                //    another cancellation exception)
                // 3. Gather any output/error that was captured before cancellation occurred
                // 4. Rethrow the OperationCanceledException to preserve cancellation semantics for the caller
                await HandleProcessCleanupAsync(process, outputTask, errorTask)
                    .ConfigureAwait(false);
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

    [ExcludeFromCodeCoverage(
        Justification = "Suppresses cancellation exceptions from output/error tasks to ensure original cancellation exception can be rethrown. Testing cancellation scenarios with async I/O operations is complex and unreliable."
    )]
    private static async Task GatherOutputSafelyAsync(
        Task<string> outputTask,
        Task<string> errorTask
    )
    {
        // These tasks were started with the same cancellationToken, so they may throw
        // OperationCanceledException/TaskCanceledException. We suppress these exceptions
        // to ensure we can rethrow the original cancellation exception from WaitForExitAsync.
        await AwaitIgnoringCancellationAsync(outputTask).ConfigureAwait(false);
        await AwaitIgnoringCancellationAsync(errorTask).ConfigureAwait(false);
    }

    [ExcludeFromCodeCoverage(
        Justification = "Handles cleanup during cancellation and timeout scenarios. Testing process termination and async output gathering is difficult to reliably reproduce."
    )]
    private static async Task HandleProcessCleanupAsync(
        Process process,
        Task<string> outputTask,
        Task<string> errorTask
    )
    {
        KillProcessSafely(process);
        await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
        await GatherOutputSafelyAsync(outputTask, errorTask).ConfigureAwait(false);
    }

    [ExcludeFromCodeCoverage(
        Justification = "Wraps process kill operation in try-catch to handle race condition where process may have already exited. Testing this specific timing scenario is difficult to reliably reproduce."
    )]
    private static void KillProcessSafely(Process process)
    {
        // The Kill operation is wrapped in a try-catch because the process may have already
        // exited by the time we attempt to kill it, which would throw an exception. We ignore
        // such exceptions as they don't affect the cleanup process.
        try
        {
            process.Kill(entireProcessTree: true);
        }
#pragma warning disable CA1031 // Do not catch general exception types
        catch (Exception)
#pragma warning restore CA1031
        {
            // Ignore errors when killing the process (e.g., process may have already exited)
        }
    }

    private static string ResolveExecutablePath(string path)
    {
        if (
            Path.IsPathRooted(path)
            || path.Contains(Path.DirectorySeparatorChar, StringComparison.Ordinal)
            || path.Contains(Path.AltDirectorySeparatorChar, StringComparison.Ordinal)
        )
        {
            return path;
        }

        if (
            OperatingSystem.IsWindows()
            && !path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
        )
        {
            return path + ".exe";
        }

        return path;
    }

    private static void SetEnvironmentVariables(
        ProcessStartInfo startInfo,
        ImmutableDictionary<string, string?> environmentVariables
    )
    {
        foreach (var (key, value) in environmentVariables)
        {
            startInfo.Environment[key] = value ?? string.Empty;
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
