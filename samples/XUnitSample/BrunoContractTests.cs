using System.Collections.Immutable;
using Teqniqly.BRUnit.Testing;
using Xunit;

namespace XUnitSample;

/// <summary>
/// Example XUnit tests demonstrating how to use Teqniqly.BRUnit.Testing
/// to run Bruno CLI contract tests.
/// </summary>
public class BrunoContractTests : IClassFixture<BrunoCollectionFixture>
{
    private readonly BrunoRunner _runner;
    private readonly string _collectionPath;

    public BrunoContractTests(BrunoCollectionFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        _runner = new BrunoRunner(new ProcessFactory());
        _collectionPath = fixture.CollectionPath;
    }

    [Fact]
    public async Task RunCollection_WithValidCollection_ReturnsSuccess()
    {
        // Arrange
        var options = new BrunoRunOptions
        {
            Target = "requests",
            EnvironmentName = "Local",
            WorkingDirectory = _collectionPath,
        };

        // Act
        var result = await _runner.RunAsync(options);

        // Assert
        Assert.True(result.IsSuccess, $"Tests failed: {result.StandardError}");
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("PASS", result.StandardOutput, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RunCollection_WithProductionEnvironment_ReturnsSuccess()
    {
        // Arrange
        var options = new BrunoRunOptions
        {
            Target = "requests",
            EnvironmentName = "production",
            WorkingDirectory = _collectionPath,
        };

        // Act
        var result = await _runner.RunAsync(options);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public async Task RunCollection_WithEnvironmentVariables_ReturnsSuccess()
    {
        // Arrange
        var options = new BrunoRunOptions
        {
            Target = "requests",
            EnvironmentName = "Local",
            EnvironmentVariables = ImmutableDictionary<string, string?>
                .Empty.Add("API_BASE_URL", "https://jsonplaceholder.typicode.com")
                .Add("API_TIMEOUT", "5000"),
            WorkingDirectory = _collectionPath,
        };

        // Act
        var result = await _runner.RunAsync(options);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task RunCollection_CompletesWithinTimeout()
    {
        // Arrange
        var timeout = TimeSpan.FromMinutes(5);
        var options = new BrunoRunOptions
        {
            Target = "requests",
            EnvironmentName = "Local",
            Timeout = timeout,
            WorkingDirectory = _collectionPath,
        };

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await _runner.RunAsync(options);
        stopwatch.Stop();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(
            stopwatch.Elapsed < timeout,
            $"Execution took {stopwatch.Elapsed.TotalSeconds:F2} seconds, which exceeds the timeout of {timeout.TotalSeconds:F2} seconds."
        );
    }

    [Fact]
    public async Task RunCollection_WhenTimeoutExceeded_ThrowsTimeoutException()
    {
        // Arrange
        // Assert
        Assert.Contains("timeout", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(
            shortTimeout.TotalSeconds.ToString(
                "F1",
                System.Globalization.CultureInfo.InvariantCulture
            ),
            exception.Message,
            StringComparison.OrdinalIgnoreCase
        );

        // Verify the timeout was enforced (should complete quickly, not wait for the full 5 seconds)
        Assert.True(
            stopwatch.Elapsed < TimeSpan.FromSeconds(1),
            $"Timeout should be enforced quickly, but took {stopwatch.Elapsed.TotalSeconds:F2} seconds"
        );
    private static string? GetTestHelperPath()
    {
        // Navigate from test assembly location to find TestHelper executable
        var assemblyLocation = Path.GetDirectoryName(typeof(BrunoContractTests).Assembly.Location);
        if (assemblyLocation == null)
        {
            return null;
        }

        // Go up from bin/Debug/net10.0 to samples directory
        var currentDir = assemblyLocation;
        for (var i = 0; i < 4 && currentDir != null; i++)
        {
            currentDir = Path.GetDirectoryName(currentDir);
        }

        if (currentDir == null)
        {
            return null;
        }

        var testHelperPath = Path.Combine(
            currentDir,
            "TestHelper",
            "bin",
            "Debug",
            "net10.0",
            OperatingSystem.IsWindows() ? "TestHelper.exe" : "TestHelper"
        );

        return testHelperPath;
    }

    [Fact]
    public async Task RunCollection_WhenCollectionFails_ReturnsFailure()
    {
        // Arrange
        // Use a non-existent target to simulate a failure
        var options = new BrunoRunOptions
        {
            Target = "nonexistent-folder",
            EnvironmentName = "Local",
            WorkingDirectory = _collectionPath,
        };

        // Act
        var result = await _runner.RunAsync(options);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotEqual(0, result.ExitCode);
    }

    [Fact]
    public async Task RunCollection_WithInvalidBrunoPath_ThrowsInvalidOperationException()
    {
        // Arrange
        var options = new BrunoRunOptions
        {
            BruExecutablePath = "nonexistent-bru-executable",
            Target = "requests",
            EnvironmentName = "Local",
            WorkingDirectory = _collectionPath,
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _runner.RunAsync(options));
    }
}
