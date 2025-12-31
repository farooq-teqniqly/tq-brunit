namespace Teqniqly.BRUnit.Testing.Tests;

// Reference: Story AC: PBI-03 AC-42 (BrunoRunner implements IBrunoRunner)
// Reference: Proposal: Section 5 (Contracts - BrunoRunner)

public class BrunoRunnerTests
{
    [Fact]
    public async Task RunAsync_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var runner = new BrunoRunner();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => runner.RunAsync(null!));
    }
}
