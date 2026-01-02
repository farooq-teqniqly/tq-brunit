using System.Diagnostics;
using System.IO;

namespace Teqniqly.BRUnit.Testing.Tests;

public class ProcessFactoryTests
{
    [Fact]
    public void Start_WhenProcessCannotBeStarted_ThrowsInvalidOperationException()
    {
        // Arrange
        var factory = new ProcessFactory();
        var expectedWorkingDirectory = Path.Combine(Path.GetTempPath(), "test", "directory");
        var startInfo = new ProcessStartInfo
        {
            FileName = "nonexistent-executable-that-does-not-exist-12345",
            Arguments = "--test-arg",
            WorkingDirectory = expectedWorkingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => factory.Start(startInfo));
        var message = exception.Message;
        Assert.Contains(
            "FileName='nonexistent-executable-that-does-not-exist-12345'",
            message,
            StringComparison.Ordinal
        );
        Assert.Contains("Arguments='--test-arg'", message, StringComparison.Ordinal);
        Assert.Contains(
            $"WorkingDirectory='{expectedWorkingDirectory}'",
            message,
            StringComparison.Ordinal
        );
        Assert.NotNull(exception.InnerException);
    }

    [Fact]
    public void Start_WithNullStartInfo_ThrowsArgumentNullException()
    {
        // Arrange
        var factory = new ProcessFactory();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => factory.Start(null!));
    }

    [Fact]
    public void Start_WithValidStartInfo_ReturnsProcess()
    {
        // Arrange
        var factory = new ProcessFactory();
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "--version",
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        // Act
        using var process = factory.Start(startInfo);

        // Assert
        Assert.NotNull(process);
    }
}
