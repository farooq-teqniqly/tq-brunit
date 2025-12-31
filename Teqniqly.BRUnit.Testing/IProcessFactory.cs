using System.Diagnostics;

namespace Teqniqly.BRUnit.Testing;

/// <summary>
/// Factory for creating process instances. Used for testability.
/// </summary>
public interface IProcessFactory
{
    /// <summary>
    /// Starts a process with the specified start info.
    /// </summary>
    /// <param name="startInfo">The process start information.</param>
    /// <returns>The started process.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the process could not be started.</exception>
    public Process Start(ProcessStartInfo startInfo);
}
