using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security;
using System.Text.RegularExpressions;
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
        // Use TestHelper executable that sleeps longer than the timeout
        var shortTimeout = TimeSpan.FromMilliseconds(200);
        var testHelperPath = GetTestHelperPath();

        Skip.If(
            testHelperPath == null || !File.Exists(testHelperPath),
            $"TestHelper executable not found at: {testHelperPath}. "
                + "Make sure TestHelper project is built before running tests. "
                + "Build the TestHelper project: dotnet build samples/TestHelper/TestHelper.csproj"
        );

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
        var exception = await Assert.ThrowsAsync<TimeoutException>(async () =>
            await _runner.RunAsync(options)
        );
        stopwatch.Stop();

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

    private static string? GetTestHelperPath()
    {
        var assemblyLocation = Path.GetDirectoryName(typeof(BrunoContractTests).Assembly.Location);
        if (assemblyLocation == null)
        {
            return null;
        }

        // Navigate up 4 levels from assembly location to reach solution root
        // Typical path: samples/XUnitSample/bin/Debug/net10.0/XUnitSample.dll
        // After navigation: solution root → TestHelper/bin/...
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
        var targetFrameworkMoniker = GetTargetFrameworkMoniker();

        // Try common build configurations first (Debug, Release)
        var commonConfigs = new[] { "Debug", "Release" };
        foreach (var config in commonConfigs)
        {
            var candidatePath = Path.Combine(
                testHelperBinDir,
                config,
                targetFrameworkMoniker,
                executableName
            );

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
                var netDir = Path.Combine(configDir, targetFrameworkMoniker);
                if (Directory.Exists(netDir))
                {
                    var candidatePath = Path.Combine(netDir, executableName);
                    if (File.Exists(candidatePath))
                    {
                        return candidatePath;
                    }
                }
            }
        }
        catch (Exception ex)
            when (ex is UnauthorizedAccessException or IOException or SecurityException)
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
            var candidatePath = Path.Combine(directory, fileName);
            if (File.Exists(candidatePath))
            {
                return candidatePath;
            }

            var subdirs = Directory.GetDirectories(directory);
            foreach (var subdir in subdirs)
            {
                var result = FindTestHelperRecursive(subdir, fileName);
                if (result != null)
                {
                    return result;
                }
            }
        }
        catch (Exception ex)
            when (ex is UnauthorizedAccessException or IOException or SecurityException)
        {
            // Expected - ignore errors during recursive search
        }

        return null;
    }

    private static string GetTargetFrameworkMoniker()
    {
        // First attempt: derive from RuntimeInformation.FrameworkDescription
        // Format: ".NET 10.0.0" or ".NET Core 8.0.0"
        var frameworkDescription = RuntimeInformation.FrameworkDescription;
        var runtimeMatch = Regex.Match(
            frameworkDescription,
            @"\.NET(?:\s+Core)?\s+(\d+)(?:\.(\d+))?(?:\.(\d+))?"
        );

        if (runtimeMatch.Success)
        {
            var major = runtimeMatch.Groups[1].Value;
            var minor = runtimeMatch.Groups[2].Success ? runtimeMatch.Groups[2].Value : "0";
            return $"net{major}.{minor}";
        }

        // Second attempt: derive from Environment.Version
        // Environment.Version provides the runtime version
        var envVersion = Environment.Version;
        if (envVersion.Major > 0)
        {
            return $"net{envVersion.Major}.{envVersion.Minor}";
        }

        // Third attempt: parse TargetFrameworkAttribute
        var assembly = typeof(BrunoContractTests).Assembly;
        var targetFrameworkAttribute =
            assembly.GetCustomAttribute<System.Runtime.Versioning.TargetFrameworkAttribute>();

        if (targetFrameworkAttribute?.FrameworkName != null)
        {
            // TargetFrameworkAttribute.FrameworkName format: ".NETCoreApp,Version=v10.0" or ".NETCoreApp,Version=v8.0.1"
            // Extract the version and convert to TFM format: "net10.0" or "net8.0"
            var frameworkName = targetFrameworkAttribute.FrameworkName;

            // Parse the version from the framework name - flexible regex handles optional minor/patch
            var versionMatch = Regex.Match(
                frameworkName,
                @"Version=v(\d+)(?:\.(\d+))?(?:\.(\d+))?"
            );

            if (versionMatch.Success)
            {
                var major = versionMatch.Groups[1].Value;
                var minor = versionMatch.Groups[2].Success ? versionMatch.Groups[2].Value : "0";
                return $"net{major}.{minor}";
            }
        }

        // If all approaches fail, throw an exception rather than returning incorrect TFM
        throw new InvalidOperationException(
            $"Unable to determine target framework moniker. "
                + $"FrameworkDescription: {RuntimeInformation.FrameworkDescription}, "
                + $"Environment.Version: {Environment.Version}, "
                + $"TargetFrameworkAttribute: {targetFrameworkAttribute?.FrameworkName ?? "null"}"
        );
    }
}
