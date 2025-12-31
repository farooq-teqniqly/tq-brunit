using System.Diagnostics;

namespace Teqniqly.BRUnit.Testing.Tests;

// Reference: Story AC: PBI-03 (ProcessFactory implementation)
// Reference: Proposal: Section 5 (Contracts - ProcessFactory)

public class ProcessFactoryTests
{
    [Fact]
    public void Start_WhenProcessCannotBeStarted_ThrowsInvalidOperationException()
    {
        // Arrange
        var factory = new ProcessFactory();
        var startInfo = new ProcessStartInfo
        {
            FileName = "nonexistent-executable-that-does-not-exist-12345",
            Arguments = "--test-arg",
            WorkingDirectory = @"C:\test\directory",
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
            "WorkingDirectory='C:\\test\\directory'",
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
