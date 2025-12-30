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
}
