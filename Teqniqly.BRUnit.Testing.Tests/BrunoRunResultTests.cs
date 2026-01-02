namespace Teqniqly.BRUnit.Testing.Tests;

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

    [Fact]
    public void Equals_WithDifferentValues_ReturnsFalse()
    {
        // Arrange
        var result1 = new BrunoRunResult { ExitCode = 0, StandardOutput = "output1" };
        var result2 = new BrunoRunResult { ExitCode = 1, StandardOutput = "output2" };

        // Act & Assert
        Assert.NotEqual(result1, result2);
        Assert.False(result1 == result2);
        Assert.True(result1 != result2);
    }

    [Fact]
    public void Equals_WithSameValues_ReturnsTrue()
    {
        var result1 = new BrunoRunResult
        {
            ExitCode = 0,
            StandardOutput = "output",
            StandardError = "error",
        };

        var result2 = new BrunoRunResult
        {
            ExitCode = 0,
            StandardOutput = "output",
            StandardError = "error",
        };

        // Act & Assert
        Assert.Equal(result1, result2);
        Assert.True(result1 == result2);
        Assert.False(result1 != result2);
    }

    [Fact]
    public void GetHashCode_WithSameValues_ReturnsSameHash()
    {
        var result1 = new BrunoRunResult
        {
            ExitCode = 0,
            StandardOutput = "output",
            StandardError = "error",
        };

        var result2 = new BrunoRunResult
        {
            ExitCode = 0,
            StandardOutput = "output",
            StandardError = "error",
        };

        // Act & Assert
        Assert.Equal(result1.GetHashCode(), result2.GetHashCode());
    }

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
        var result = new BrunoRunResult { ExitCode = exitCode };

        Assert.Equal(expectedIsSuccess, result.IsSuccess);
    }
}
