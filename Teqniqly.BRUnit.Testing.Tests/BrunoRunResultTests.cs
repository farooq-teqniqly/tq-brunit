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

    // Reference: Story AC: PBI-02 AC-42, AC-43 (IsSuccess logic)
    // Reference: Proposal: Section 5 (Contracts - BrunoRunResult)
    [Fact]
    public void IsSuccess_WithExitCodeZero_ReturnsTrue()
    {
        // Arrange
        var result = new BrunoRunResult { ExitCode = 0 };

        // Act & Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void IsSuccess_WithNonZeroExitCode_ReturnsFalse()
    {
        // Arrange
        var result = new BrunoRunResult { ExitCode = 1 };

        // Act & Assert
        Assert.False(result.IsSuccess);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(-1, false)]
    [InlineData(255, false)]
    public void IsSuccess_WithVariousExitCodes_ReturnsExpected(int exitCode, bool expectedIsSuccess)
    {
        // Arrange
        var result = new BrunoRunResult { ExitCode = exitCode };

        // Act & Assert
        Assert.Equal(expectedIsSuccess, result.IsSuccess);
    }
}
