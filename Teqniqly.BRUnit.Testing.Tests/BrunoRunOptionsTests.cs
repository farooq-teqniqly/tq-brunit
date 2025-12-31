namespace Teqniqly.BRUnit.Testing.Tests;

// Reference: Story AC: PBI-02 AC-40 (default values)
// Reference: Proposal: Section 5 (Contracts - BrunoRunOptions)
// Reference: Spec: Section 4 (Core API - BrunoRunOptions)

public class BrunoRunOptionsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new BrunoRunOptions();

        // Assert
        Assert.Equal("bru", options.BruExecutablePath);
        Assert.Equal(Directory.GetCurrentDirectory(), options.WorkingDirectory);
        Assert.Equal(string.Empty, options.Target);
        Assert.Null(options.EnvironmentName);
        Assert.Equal(TimeSpan.FromMinutes(2), options.Timeout);
        Assert.NotNull(options.EnvironmentVariables);
        Assert.IsType<Dictionary<string, string?>>(options.EnvironmentVariables);

        // Verify case-insensitive comparer
        options.EnvironmentVariables["TEST"] = "value1";
        Assert.Equal("value1", options.EnvironmentVariables["test"]); // Should find by case-insensitive key
        Assert.Equal("value1", options.EnvironmentVariables["TEST"]); // Should find by original key
    }

    // Note: Immutability (AC-44) is enforced at compile-time by the record type and init-only accessors.
    // The compiler prevents assignment after object initialization, so runtime tests are not needed.
    // Value preservation is already verified in DefaultValues_AreCorrect().

    [Fact]
    public void Equals_WithDifferentEnvironmentVariables_ReturnsFalse()
    {
        // Arrange
        var options1 = new BrunoRunOptions
        {
            Target = "test.bru",
            EnvironmentVariables = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["KEY1"] = "value1",
            },
        };

        var options2 = new BrunoRunOptions
        {
            Target = "test.bru",
            EnvironmentVariables = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["KEY2"] = "value2",
            },
        };

        // Act & Assert
        Assert.NotEqual(options1, options2);
    }

    [Fact]
    public void Equals_WithDifferentValues_ReturnsFalse()
    {
        // Arrange
        var options1 = new BrunoRunOptions { Target = "test1.bru" };

        var options2 = new BrunoRunOptions { Target = "test2.bru" };

        // Act & Assert
        Assert.NotEqual(options1, options2);
        Assert.False(options1 == options2);
        Assert.True(options1 != options2);
    }

    // Reference: Story AC: PBI-02 AC-48 (value equality)
    // Reference: Proposal: Section 5 (Contracts - BrunoRunOptions)
    [Fact]
    public void Equals_WithSameValues_ReturnsTrue()
    {
        // Arrange - Use same dictionary instance for reference equality
        var envVars = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var options1 = new BrunoRunOptions
        {
            BruExecutablePath = "bru",
            Target = "test.bru",
            EnvironmentName = "dev",
            Timeout = TimeSpan.FromMinutes(5),
            WorkingDirectory = "/path",
            EnvironmentVariables = envVars,
        };

        var options2 = new BrunoRunOptions
        {
            BruExecutablePath = "bru",
            Target = "test.bru",
            EnvironmentName = "dev",
            Timeout = TimeSpan.FromMinutes(5),
            WorkingDirectory = "/path",
            EnvironmentVariables = envVars,
        };

        // Act & Assert
        Assert.Equal(options1, options2);
        Assert.True(options1 == options2);
        Assert.False(options1 != options2);
    }

    [Fact]
    public void GetHashCode_WithSameValues_ReturnsSameHash()
    {
        // Arrange - Use same dictionary instance for reference equality
        var envVars = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var options1 = new BrunoRunOptions
        {
            BruExecutablePath = "bru",
            Target = "test.bru",
            EnvironmentName = "dev",
            Timeout = TimeSpan.FromMinutes(5),
            WorkingDirectory = "/path",
            EnvironmentVariables = envVars,
        };

        var options2 = new BrunoRunOptions
        {
            BruExecutablePath = "bru",
            Target = "test.bru",
            EnvironmentName = "dev",
            Timeout = TimeSpan.FromMinutes(5),
            WorkingDirectory = "/path",
            EnvironmentVariables = envVars,
        };

        // Act & Assert
        Assert.Equal(options1.GetHashCode(), options2.GetHashCode());
    }
}
