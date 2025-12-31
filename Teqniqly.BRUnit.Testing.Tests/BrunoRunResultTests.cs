namespace Teqniqly.BRUnit.Testing.Tests;

// Reference: Story AC: PBI-02 AC-41 (BrunoRunResult is sealed record with all properties)
// Reference: Proposal: Section 5 (Contracts - BrunoRunResult)
// Reference: Spec: Section 4 (Core API - BrunoRunResult)

public class BrunoRunResultTests
{
    [Fact]
    public void CanCreate_WithAllProperties()
    {
        // Arrange & Act
        var result = new BrunoRunResult
        {
            ExitCode = 0,
            StandardOutput = "output text",
            StandardError = "error text",
        };

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("output text", result.StandardOutput);
        Assert.Equal("error text", result.StandardError);
    }
}
