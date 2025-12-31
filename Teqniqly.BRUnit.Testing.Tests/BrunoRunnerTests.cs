using System.Diagnostics;
using NSubstitute;

namespace Teqniqly.BRUnit.Testing.Tests;

// Reference: Story AC: PBI-03 AC-42 (BrunoRunner implements IBrunoRunner)
// Reference: Proposal: Section 5 (Contracts - BrunoRunner)

public class BrunoRunnerTests
{
    [Fact]
    public void Constructor_WithNullProcessFactory_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new BrunoRunner(null!));
    }

    [Fact]
    public async Task RunAsync_WhenProcessFactoryThrows_PropagatesException()
    {
        // Arrange
        var processFactory = Substitute.For<IProcessFactory>();
        processFactory
            .Start(Arg.Any<ProcessStartInfo>())
            .Returns(x =>
                throw new InvalidOperationException("Failed to start process: nonexistent")
            );

        var options = new BrunoRunOptions { BruExecutablePath = "nonexistent", Target = "test" };
        var runner = new BrunoRunner(processFactory);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            runner.RunAsync(options)
        );

        Assert.Contains("nonexistent", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunAsync_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var runner = new BrunoRunner(new ProcessFactory());

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => runner.RunAsync(null!));
    }

    [Fact]
    public async Task RunAsync_WithValidOptions_ExecutesProcessSuccessfully()
    {
        // Arrange
        var options = new BrunoRunOptions { BruExecutablePath = "dotnet", Target = "--version" };
        var runner = new BrunoRunner(new ProcessFactory());

        // Act
        var result = await runner.RunAsync(options);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.ExitCode);
    }
}
