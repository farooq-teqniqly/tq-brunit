using System.Collections.Immutable;
using Teqniqly.BRUnit.Testing;

namespace NUnitSample;

/// <summary>
/// Example NUnit tests demonstrating how to use Teqniqly.BRUnit.Testing
/// to run Bruno CLI contract tests.
/// </summary>
[TestFixture]
public class BrunoContractTests
{
    private BrunoRunner _runner = null!;
    private string _collectionPath = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        var fixture = new BrunoCollectionFixture();
        ArgumentNullException.ThrowIfNull(fixture);
        _runner = new BrunoRunner(new ProcessFactory());
        _collectionPath = fixture.CollectionPath;
    }

    [Test]
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
        Assert.That(result.IsSuccess, Is.True, $"Tests failed: {result.StandardError}");
        Assert.That(result.ExitCode, Is.EqualTo(0));
        Assert.That(result.StandardOutput, Does.Contain("PASS").IgnoreCase);
    }

    [Test]
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
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.ExitCode, Is.EqualTo(0));
    }

    [Test]
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
        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
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
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(
            stopwatch.Elapsed,
            Is.LessThan(timeout),
            $"Execution took {stopwatch.Elapsed.TotalSeconds:F2} seconds, which exceeds the timeout of {timeout.TotalSeconds:F2} seconds."
        );
    }

    [Test]
    public async Task RunCollection_WhenTimeoutExceeded_ThrowsTimeoutException()
    {
        // Arrange
        // Use TestHelper executable that sleeps longer than the timeout
        var shortTimeout = TimeSpan.FromMilliseconds(200);
        var testHelperPath = GetTestHelperPath();

        if (testHelperPath == null || !File.Exists(testHelperPath))
        {
            Assert.Fail(
                $"TestHelper executable not found at: {testHelperPath}. "
                    + "Make sure TestHelper project is built before running tests."
            );
            return;
        }

        // TestHelper sleeps for 5 seconds, which exceeds the 200ms timeout
        // BrunoRunner will call: TestHelper.exe run 5
        var options = new BrunoRunOptions
        {
            BruExecutablePath = testHelperPath,
            Target = "5", // Sleep for 5 seconds (TestHelper will parse this after "run")
            Timeout = shortTimeout,
        };

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var exception = Assert.ThrowsAsync<TimeoutException>(async () =>
            await _runner.RunAsync(options)
        );
        stopwatch.Stop();

        // Assert
        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.Message, Does.Contain("timeout").IgnoreCase);
        Assert.That(
            exception.Message,
            Does.Contain(
                shortTimeout.TotalSeconds.ToString(
                    "F1",
                    System.Globalization.CultureInfo.InvariantCulture
                )
            ).IgnoreCase
        );

        // Verify the timeout was enforced (should complete quickly, not wait for the full 5 seconds)
        Assert.That(
            stopwatch.Elapsed,
            Is.LessThan(TimeSpan.FromSeconds(1)),
            $"Timeout should be enforced quickly, but took {stopwatch.Elapsed.TotalSeconds:F2} seconds"
        );
    }

    [Test]
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
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.ExitCode, Is.Not.EqualTo(0));
    }

    [Test]
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
        Assert.ThrowsAsync<InvalidOperationException>(() => _runner.RunAsync(options));
    }

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
}
