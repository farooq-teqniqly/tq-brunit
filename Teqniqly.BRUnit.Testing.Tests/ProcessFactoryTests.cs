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
            Arguments = string.Empty,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => factory.Start(startInfo));
        Assert.Contains(
            "nonexistent-executable-that-does-not-exist-12345",
            exception.Message,
            StringComparison.Ordinal
        );
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
        var process = factory.Start(startInfo);

        // Assert
        Assert.NotNull(process);
        process.Dispose();
    }
}
