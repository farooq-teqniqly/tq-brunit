using System.Collections.Immutable;
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

    private static string BuildArguments(BrunoRunOptions options)
    {
        var args = new List<string> { "run" };

        // Add --env flag if environment name is specified
        if (!string.IsNullOrWhiteSpace(options.EnvironmentName))
        {
            args.Add("--env");
            args.Add(options.EnvironmentName);
        }

        // Add target (file or folder) as the last argument
        args.Add(options.Target);

        return string.Join(" ", args.Select(arg => EscapeArgument(arg)));
    }

    private static ProcessStartInfo CreateProcessStartInfo(BrunoRunOptions options)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = options.BruExecutablePath,
            Arguments = BuildArguments(options),
            WorkingDirectory = options.WorkingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        // Add environment variables from options
        SetEnvironmentVariables(startInfo, options.EnvironmentVariables);

        return startInfo;
    }

    private static string EscapeArgument(string argument)
    {
        // Escape arguments that contain spaces or special characters
        if (argument.Contains(' ', StringComparison.Ordinal))
        {
            return $"\"{argument.Replace("\"", "\\\"", StringComparison.Ordinal)}\"";
        }
        return argument;
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
                await HandleCancellationAsync(process, outputTask, errorTask).ConfigureAwait(false);
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

    private static async Task HandleCancellationAsync(
        Process process,
        Task<string> outputTask,
        Task<string> errorTask
    )
    {
        KillProcessSafely(process);
        await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
        await GatherOutputSafelyAsync(outputTask, errorTask).ConfigureAwait(false);
    }

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
            // Ignore errors when killing the process (e.g., already exited)
        }
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
