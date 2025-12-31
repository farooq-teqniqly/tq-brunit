using System.ComponentModel;
using System.Diagnostics;

namespace Teqniqly.BRUnit.Testing;

/// <summary>
/// Default implementation of <see cref="IProcessFactory"/> that uses <see cref="Process.Start(ProcessStartInfo)"/>.
/// </summary>
public sealed class ProcessFactory : IProcessFactory
{
    /// <inheritdoc />
    public Process Start(ProcessStartInfo startInfo)
    {
        ArgumentNullException.ThrowIfNull(startInfo);

        try
        {
            var process = Process.Start(startInfo);
            // On Unix systems, Process.Start can return null when the process cannot be started.
            // On Windows, Process.Start throws Win32Exception instead.
            // We handle both cases to ensure consistent InvalidOperationException behavior across platforms.
            if (process == null)
            {
                throw new InvalidOperationException(BuildProcessStartErrorMessage(startInfo));
            }

            return process;
        }
        catch (Win32Exception ex)
        {
            // On Windows, Process.Start throws Win32Exception when the process cannot be started.
            // Wrap it in InvalidOperationException for consistent behavior across platforms.
            throw new InvalidOperationException(BuildProcessStartErrorMessage(startInfo), ex);
        }
    }

    private static string BuildProcessStartErrorMessage(ProcessStartInfo startInfo)
    {
        var message = $"Failed to start process: FileName='{startInfo.FileName}'";

        var arguments = GetArgumentsString(startInfo);
        if (!string.IsNullOrWhiteSpace(arguments))
        {
            message += $", Arguments='{arguments}'";
        }

        if (!string.IsNullOrWhiteSpace(startInfo.WorkingDirectory))
        {
            message += $", WorkingDirectory='{startInfo.WorkingDirectory}'";
        }

        return message;
    }

    private static string GetArgumentsString(ProcessStartInfo startInfo)
    {
        // Prefer ArgumentList if it has items, as it's more explicit
        if (startInfo.ArgumentList.Count > 0)
        {
            return string.Join(" ", startInfo.ArgumentList);
        }

        // Fall back to Arguments property
        return startInfo.Arguments ?? string.Empty;
    }
}
