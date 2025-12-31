using System.Collections.Immutable;
using System.Diagnostics;
using NSubstitute;

namespace Teqniqly.BRUnit.Testing.Tests;

// Reference: Story AC: PBI-03 AC-42 (BrunoRunner implements IBrunoRunner)
// Reference: Proposal: Section 5 (Contracts - BrunoRunner)

public class BrunoRunnerTests
{
    [Fact]
    public void Constructor_WithNullProcessFactory_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new BrunoRunner(null!));
    }

    [Fact]
    public async Task RunAsync_BuildsCorrectArguments_WithEnvironmentName()
    {
        // Arrange
        var options = new BrunoRunOptions
        {
            BruExecutablePath = "dotnet",
            Target = "test.bru",
            EnvironmentName = "production",
        };
        var processFactory = Substitute.For<IProcessFactory>();
        ProcessStartInfo? capturedStartInfo = null;
        processFactory
            .Start(Arg.Any<ProcessStartInfo>())
            .Returns(callInfo =>
            {
                capturedStartInfo = callInfo.Arg<ProcessStartInfo>();
                var startInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };
                return Process.Start(startInfo)!;
            });
        var runner = new BrunoRunner(processFactory);

        // Act
        await runner.RunAsync(options);

        // Assert
        Assert.NotNull(capturedStartInfo);
        Assert.Equal(4, capturedStartInfo.ArgumentList.Count);
        Assert.Equal("run", capturedStartInfo.ArgumentList[0]);
        Assert.Equal("--env", capturedStartInfo.ArgumentList[1]);
        Assert.Equal("production", capturedStartInfo.ArgumentList[2]);
        Assert.Equal("test.bru", capturedStartInfo.ArgumentList[3]);
    }

    [Fact]
    public async Task RunAsync_BuildsCorrectArguments_WithoutEnvironmentName()
    {
        // Arrange
        var options = new BrunoRunOptions { BruExecutablePath = "dotnet", Target = "test.bru" };
        var processFactory = Substitute.For<IProcessFactory>();
        ProcessStartInfo? capturedStartInfo = null;
        processFactory
            .Start(Arg.Any<ProcessStartInfo>())
            .Returns(callInfo =>
            {
                capturedStartInfo = callInfo.Arg<ProcessStartInfo>();
                var startInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };
                return Process.Start(startInfo)!;
            });
        var runner = new BrunoRunner(processFactory);

        // Act
        await runner.RunAsync(options);

        // Assert
        Assert.NotNull(capturedStartInfo);
        Assert.Equal(2, capturedStartInfo.ArgumentList.Count);
        Assert.Equal("run", capturedStartInfo.ArgumentList[0]);
        Assert.Equal("test.bru", capturedStartInfo.ArgumentList[1]);
    }

    [Fact]
    public async Task RunAsync_CapturesStandardError()
    {
        // Arrange
        // Use dotnet with an invalid command to produce stderr output
        var options = new BrunoRunOptions
        {
            BruExecutablePath = "dotnet",
            Target = "invalidcommand",
        };
        var runner = new BrunoRunner(new ProcessFactory());

        // Act
        var result = await runner.RunAsync(options);

        // Assert
        Assert.NotNull(result.StandardError);
        Assert.NotEmpty(result.StandardError);
    }

    [Fact]
    public async Task RunAsync_CapturesStandardOutput()
    {
        // Arrange
        // Use echo as a simple test command that produces output
        var options = new BrunoRunOptions
        {
            BruExecutablePath = OperatingSystem.IsWindows() ? "cmd" : "echo",
            Target = OperatingSystem.IsWindows() ? "/c echo test-output" : "test-output",
        };
        var runner = new BrunoRunner(new ProcessFactory());

        // Act
        var result = await runner.RunAsync(options);

        // Assert
        Assert.NotEmpty(result.StandardOutput);
        Assert.Contains("test-output", result.StandardOutput, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunAsync_EscapesArguments_WithSpaces()
    {
        // Arrange
        var options = new BrunoRunOptions
        {
            BruExecutablePath = "dotnet",
            Target = "test folder/test file.bru",
            EnvironmentName = "my environment",
        };
        var processFactory = Substitute.For<IProcessFactory>();
        ProcessStartInfo? capturedStartInfo = null;
        processFactory
            .Start(Arg.Any<ProcessStartInfo>())
            .Returns(callInfo =>
            {
                capturedStartInfo = callInfo.Arg<ProcessStartInfo>();
                var startInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };
                return Process.Start(startInfo)!;
            });
        var runner = new BrunoRunner(processFactory);

        // Act
        await runner.RunAsync(options);

        // Assert
        Assert.NotNull(capturedStartInfo);
        // Arguments in ArgumentList (runtime handles escaping)
        Assert.Equal(4, capturedStartInfo.ArgumentList.Count);
        Assert.Equal("run", capturedStartInfo.ArgumentList[0]);
        Assert.Equal("--env", capturedStartInfo.ArgumentList[1]);
        Assert.Equal("my environment", capturedStartInfo.ArgumentList[2]);
        Assert.Equal("test folder/test file.bru", capturedStartInfo.ArgumentList[3]);
    }

    [Fact]
    public async Task RunAsync_PassesEnvironmentVariables()
    {
        // Arrange
        var envVars = ImmutableDictionary<string, string?>
            .Empty.WithComparers(StringComparer.OrdinalIgnoreCase)
            .Add("TEST_VAR_1", "value1")
            .Add("TEST_VAR_2", "value2");

        // Use a command that prints environment variables
        // BrunoRunner always prepends "run", so we need a command that handles this
        // On Linux: The challenge is that commands like "sh run -c ..." are invalid syntax
        // Solution: Use '/usr/bin/env' with a command that will print environment variables
        // The command becomes: /usr/bin/env run <target>
        // Since 'env' will try to execute "run" as a command (which doesn't exist), it will fail
        // But the environment variables are still set on the process, so we can check if they're passed
        // Actually, we need a command that will actually execute and show the env vars
        // Best solution: Use a command that accepts "run" as a valid first argument, or use a wrapper
        // We'll use '/usr/bin/env' with 'printenv' to print all env vars, making "run" part of the command
        // The command becomes: /usr/bin/env run printenv
        // But env will try to execute "run" as a command.
        // Final solution: Use a command that will work with "run" as the first argument
        // Let's use '/usr/bin/env' with a command that prints env vars, accepting that "run" will fail
        // and check if the env vars are in the error output or use a different verification method
        var options = new BrunoRunOptions
        {
            BruExecutablePath = OperatingSystem.IsWindows() ? "cmd" : "/usr/bin/env",
            Target = OperatingSystem.IsWindows() ? "/c echo %TEST_VAR_1% %TEST_VAR_2%" : "printenv", // /usr/bin/env run printenv - env will try to execute "run" as command, but printenv might still run
            EnvironmentVariables = envVars,
        };
        var runner = new BrunoRunner(new ProcessFactory());

        // Act
        var result = await runner.RunAsync(options);

        // Assert
        // Note: On Linux, the command may fail (non-zero exit code) because "run" doesn't exist as a command
        // But the environment variables should still be passed to the process and may appear in output/error
        // We check both StandardOutput and StandardError for the values, and accept non-zero exit codes
        var outputContainsValues =
            (
                result.StandardOutput.Contains("value1", StringComparison.Ordinal)
                && result.StandardOutput.Contains("value2", StringComparison.Ordinal)
            )
            || (
                result.StandardError.Contains("value1", StringComparison.Ordinal)
                && result.StandardError.Contains("value2", StringComparison.Ordinal)
            );

        Assert.True(
            result.IsSuccess || outputContainsValues,
            $"Process failed with ExitCode: {result.ExitCode}. "
                + $"StandardOutput: '{result.StandardOutput}'. "
                + $"StandardError: '{result.StandardError}'. "
                + $"Output contains value1: {result.StandardOutput.Contains("value1", StringComparison.Ordinal)}, "
                + $"value2: {result.StandardOutput.Contains("value2", StringComparison.Ordinal)}. "
                + $"Error contains value1: {result.StandardError.Contains("value1", StringComparison.Ordinal)}, "
                + $"value2: {result.StandardError.Contains("value2", StringComparison.Ordinal)}"
        );

        // Check that at least one of output or error contains the values
        var hasValue1 =
            result.StandardOutput.Contains("value1", StringComparison.Ordinal)
            || result.StandardError.Contains("value1", StringComparison.Ordinal);
        var hasValue2 =
            result.StandardOutput.Contains("value2", StringComparison.Ordinal)
            || result.StandardError.Contains("value2", StringComparison.Ordinal);

        Assert.True(
            hasValue1,
            $"Expected 'value1' in output or error. Output: '{result.StandardOutput}', Error: '{result.StandardError}'"
        );
        Assert.True(
            hasValue2,
            $"Expected 'value2' in output or error. Output: '{result.StandardOutput}', Error: '{result.StandardError}'"
        );
    }

    [Fact]
    public async Task RunAsync_WhenProcessFactoryThrows_PropagatesException()
    {
        // Arrange
        var processFactory = Substitute.For<IProcessFactory>();
        processFactory
            .Start(Arg.Any<ProcessStartInfo>())
            .Returns(_ =>
                throw new InvalidOperationException("Failed to start process: nonexistent")
            );

        var options = new BrunoRunOptions { BruExecutablePath = "nonexistent", Target = "test" };
        var runner = new BrunoRunner(processFactory);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            runner.RunAsync(options)
        );

        Assert.Contains("nonexistent", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunAsync_WithCustomWorkingDirectory_PassesToProcessStartInfo()
    {
        // Arrange
        var expectedWorkingDirectory = Path.GetTempPath();
        var options = new BrunoRunOptions
        {
            BruExecutablePath = "dotnet",
            Target = "--version",
            WorkingDirectory = expectedWorkingDirectory,
        };
        var processFactory = Substitute.For<IProcessFactory>();
        ProcessStartInfo? capturedStartInfo = null;
        processFactory
            .Start(Arg.Any<ProcessStartInfo>())
            .Returns(callInfo =>
            {
                capturedStartInfo = callInfo.Arg<ProcessStartInfo>();
                var startInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };
                return Process.Start(startInfo)!;
            });
        var runner = new BrunoRunner(processFactory);

        // Act
        await runner.RunAsync(options);

        // Assert
        Assert.NotNull(capturedStartInfo);
        Assert.Equal(expectedWorkingDirectory, capturedStartInfo.WorkingDirectory);
    }

    [Fact]
    public async Task RunAsync_WithEmptyBruExecutablePath_ThrowsArgumentException()
    {
        // Arrange
        var options = new BrunoRunOptions { BruExecutablePath = string.Empty, Target = "test" };
        var runner = new BrunoRunner(new ProcessFactory());

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => runner.RunAsync(options));
        Assert.Contains("BruExecutablePath", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunAsync_WithEmptyEnvironmentVariables_DoesNotThrow()
    {
        // Arrange
        var options = new BrunoRunOptions
        {
            BruExecutablePath = "dotnet",
            Target = "test.bru",
            EnvironmentVariables = ImmutableDictionary<string, string?>.Empty.WithComparers(
                StringComparer.OrdinalIgnoreCase
            ),
        };
        var processFactory = Substitute.For<IProcessFactory>();
        processFactory
            .Start(Arg.Any<ProcessStartInfo>())
            .Returns(_ =>
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };
                return Process.Start(startInfo)!;
            });
        var runner = new BrunoRunner(processFactory);

        // Act
        var result = await runner.RunAsync(options);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task RunAsync_WithEmptyTarget_ThrowsArgumentException()
    {
        // Arrange
        var options = new BrunoRunOptions { BruExecutablePath = "bru", Target = string.Empty };
        var runner = new BrunoRunner(new ProcessFactory());

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => runner.RunAsync(options));
        Assert.Contains("Target", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunAsync_WithEnvironmentVariables_PassesToProcessStartInfo()
    {
        // Arrange
        var expectedEnvVars = ImmutableDictionary<string, string?>
            .Empty.WithComparers(StringComparer.OrdinalIgnoreCase)
            .Add("CUSTOM_VAR", "custom_value")
            .Add("ANOTHER_VAR", "another_value");

        var options = new BrunoRunOptions
        {
            BruExecutablePath = "dotnet",
            Target = "--version",
            EnvironmentVariables = expectedEnvVars,
        };
        var processFactory = Substitute.For<IProcessFactory>();
        ProcessStartInfo? capturedStartInfo = null;
        processFactory
            .Start(Arg.Any<ProcessStartInfo>())
            .Returns(callInfo =>
            {
                capturedStartInfo = callInfo.Arg<ProcessStartInfo>();
                var startInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };
                return Process.Start(startInfo)!;
            });
        var runner = new BrunoRunner(processFactory);

        // Act
        await runner.RunAsync(options);

        // Assert
        Assert.NotNull(capturedStartInfo);
        Assert.Equal("custom_value", capturedStartInfo.Environment["CUSTOM_VAR"]);
        Assert.Equal("another_value", capturedStartInfo.Environment["ANOTHER_VAR"]);
    }

    [Fact]
    public async Task RunAsync_WithNullEnvironmentVariableValue_ConvertsToEmptyString()
    {
        // Arrange
        var envVars = ImmutableDictionary<string, string?>
            .Empty.WithComparers(StringComparer.OrdinalIgnoreCase)
            .Add("NULL_VAR", null);

        var options = new BrunoRunOptions
        {
            BruExecutablePath = "dotnet",
            Target = "--version",
            EnvironmentVariables = envVars,
        };
        var processFactory = Substitute.For<IProcessFactory>();
        ProcessStartInfo? capturedStartInfo = null;
        processFactory
            .Start(Arg.Any<ProcessStartInfo>())
            .Returns(callInfo =>
            {
                capturedStartInfo = callInfo.Arg<ProcessStartInfo>();
                var startInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };
                return Process.Start(startInfo)!;
            });
        var runner = new BrunoRunner(processFactory);

        // Act
        await runner.RunAsync(options);

        // Assert
        Assert.NotNull(capturedStartInfo);
        Assert.Equal(string.Empty, capturedStartInfo.Environment["NULL_VAR"]);
    }

    [Fact]
    public async Task RunAsync_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var runner = new BrunoRunner(new ProcessFactory());

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => runner.RunAsync(null!));
    }

    [SkippableFact]
    public async Task RunAsync_WithRealBrunoCli_ExecutesSuccessfully()
    {
        // Arrange
        var bruPath = FindBrunoCliPath();
        Skip.If(string.IsNullOrEmpty(bruPath), "Bruno CLI not available");

        // Use bru run -h to get help output (this is a simple command that will succeed)
        // Note: For a real integration test with actual test execution,
        // you would need an actual .bru file or collection folder
        var options = new BrunoRunOptions { BruExecutablePath = bruPath, Target = "-h" };
        var runner = new BrunoRunner(new ProcessFactory());

        // Act
        var result = await runner.RunAsync(options).ConfigureAwait(false);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.ExitCode);
        Assert.NotEmpty(result.StandardOutput);
    }

    [Fact]
    public async Task RunAsync_WithValidOptions_ExecutesProcessSuccessfully()
    {
        // Arrange
        // Use echo as a simple test command
        var options = new BrunoRunOptions
        {
            BruExecutablePath = OperatingSystem.IsWindows() ? "cmd" : "echo",
            Target = OperatingSystem.IsWindows() ? "/c echo test" : "test",
        };
        var runner = new BrunoRunner(new ProcessFactory());

        // Act
        var result = await runner.RunAsync(options);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public async Task RunAsync_WithWhitespaceBruExecutablePath_ThrowsArgumentException()
    {
        // Arrange
        var options = new BrunoRunOptions { BruExecutablePath = "   ", Target = "test" };
        var runner = new BrunoRunner(new ProcessFactory());

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => runner.RunAsync(options));
        Assert.Contains("BruExecutablePath", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunAsync_WithWhitespaceTarget_ThrowsArgumentException()
    {
        // Arrange
        var options = new BrunoRunOptions { BruExecutablePath = "bru", Target = "   " };
        var runner = new BrunoRunner(new ProcessFactory());

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => runner.RunAsync(options));
        Assert.Contains("Target", exception.Message, StringComparison.Ordinal);
    }

    private static string? FindBrunoCliPath()
    {
        // Try to find bru executable
        // On Windows, npm global binaries are often in AppData\Roaming\npm, which may not be
        // in the PATH for test processes. Try common locations first, then fall back to PATH.
        var bruPaths = new List<string>();

        if (OperatingSystem.IsWindows())
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            bruPaths.Add(Path.Combine(appData, "npm", "bru.cmd"));
            bruPaths.Add(Path.Combine(appData, "npm", "bru"));
        }

        bruPaths.Add("bru");

        foreach (var bruPath in bruPaths)
        {
            if (TryRunBrunoVersion(bruPath))
            {
                return bruPath;
            }
        }

        return null;
    }

    private static bool TryRunBrunoVersion(string bruPath)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = bruPath,
                Arguments = "--version",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            using var process = Process.Start(startInfo);
            if (process == null)
            {
                return false;
            }

            process.WaitForExit();
            return process.ExitCode == 0
                || !string.IsNullOrEmpty(process.StandardOutput.ReadToEnd());
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
#pragma warning disable CA1031 // Do not catch general exception types
        catch (Exception)
#pragma warning restore CA1031
        {
            return false;
        }
    }
}
