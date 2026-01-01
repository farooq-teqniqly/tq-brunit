using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.Versioning;
using Teqniqly.BRUnit.Testing;

namespace NUnitSample;

/// <summary>
/// Example NUnit tests demonstrating how to use Teqniqly.BRUnit.Testing
/// to run Bruno CLI contract tests.
/// </summary>
[TestFixture]
public class BrunoContractTests
{
    private static readonly string TargetFrameworkMoniker = GetTargetFrameworkMoniker();

    private BrunoRunner _runner = null!;
    private string _collectionPath = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        var fixture = new BrunoCollectionFixture();
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

        var testHelperBinDir = Path.Combine(currentDir, "TestHelper", "bin");
        if (!Directory.Exists(testHelperBinDir))
        {
            return null;
        }

        var executableName = OperatingSystem.IsWindows() ? "TestHelper.exe" : "TestHelper";

        // Try common build configurations first (Debug, Release)
        var commonConfigs = new[] { "Debug", "Release" };
        foreach (var config in commonConfigs)
        {
            var candidatePath = Path.Combine(testHelperBinDir, config, TargetFrameworkMoniker, executableName);

            if (File.Exists(candidatePath))
            {
                return candidatePath;
            }
        }

        // If not found in common configs, enumerate subfolders under bin
        try
        {
            var configDirs = Directory.GetDirectories(testHelperBinDir);
            foreach (var configDir in configDirs)
            {
                var netDir = Path.Combine(configDir, TargetFrameworkMoniker);
                if (Directory.Exists(netDir))
                {
                    var candidatePath = Path.Combine(netDir, executableName);
                    if (File.Exists(candidatePath))
                    {
        }
        catch (UnauthorizedAccessException)
        {
            // If directory enumeration fails due to permissions, fall through to recursive search
        }
        catch (Exception ex) when (ex is IOException or SecurityException)
        {
            // If directory enumeration fails, fall through to recursive search
        }        }
        catch
        {
            // If directory enumeration fails, fall through to recursive search
        }

        // Fall back to recursive search under TestHelper/bin
        return FindTestHelperRecursive(testHelperBinDir, executableName);
    }

    private static string? FindTestHelperRecursive(string directory, string fileName)
    {
        if (!Directory.Exists(directory))
        {
            return null;
        }

        try
        {
            // Check current directory for the executable
            var candidatePath = Path.Combine(directory, fileName);
            if (File.Exists(candidatePath))
            {
                return candidatePath;
            }

            // Recursively search subdirectories
            var subdirs = Directory.GetDirectories(directory);
        try
        {
            // Recursive search logic here
        }
        catch (UnauthorizedAccessException)
        {
            // Ignore errors during recursive search
        }
        catch (Exception ex) when (ex is IOException or SecurityException)
        {
            // Ignore I/O and security errors during recursive search
        }                {
                    return result;
                }
            }
        }
        catch
        {
            // Ignore errors during recursive search
        }

        return null;
    }

    private static string GetTargetFrameworkMoniker()
    {
        var assembly = typeof(BrunoContractTests).Assembly;
        var targetFrameworkAttribute = assembly.GetCustomAttribute<TargetFrameworkAttribute>();

        if (targetFrameworkAttribute?.FrameworkName != null)
        {
            // TargetFrameworkAttribute.FrameworkName format: ".NETCoreApp,Version=v10.0"
            // Extract the version and convert to TFM format: "net10.0"
            var frameworkName = targetFrameworkAttribute.FrameworkName;

            // Parse the version from the framework name
            var versionMatch = System.Text.RegularExpressions.Regex.Match(
                frameworkName,
                @"Version=v(\d+)\.(\d+)"
            );

            if (versionMatch.Success)
            {
                var major = versionMatch.Groups[1].Value;
                var minor = versionMatch.Groups[2].Value;
                return $"net{major}.{minor}";
            }
        }

        // Fallback to hardcoded value if parsing fails
        return "net10.0";
    }
}
