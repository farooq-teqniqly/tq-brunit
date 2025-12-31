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
                throw new InvalidOperationException(
                    $"Failed to start process: {startInfo.FileName}"
                );
            }

            return process;
        }
        catch (Win32Exception ex)
        {
            // On Windows, Process.Start throws Win32Exception when the process cannot be started.
            // Wrap it in InvalidOperationException for consistent behavior across platforms.
            throw new InvalidOperationException(
                $"Failed to start process: {startInfo.FileName}",
                ex
            );
        }
    }
}
