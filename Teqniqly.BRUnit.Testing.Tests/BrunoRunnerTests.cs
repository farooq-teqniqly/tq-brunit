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
                // Return a real process that will complete quickly
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
                // Return a real process that will complete quickly
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
        // Arguments will be: "run test-output"
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
                // Return a real process that will complete quickly
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
        // Arguments should be in ArgumentList (runtime handles escaping)
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

        var options = new BrunoRunOptions
        {
            BruExecutablePath = OperatingSystem.IsWindows() ? "cmd" : "sh",
            Target = OperatingSystem.IsWindows()
                ? "/c echo %TEST_VAR_1% %TEST_VAR_2%"
                : "-c \"echo $TEST_VAR_1 $TEST_VAR_2\"",
            EnvironmentVariables = envVars,
        };
        var runner = new BrunoRunner(new ProcessFactory());

        // Act
        var result = await runner.RunAsync(options);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Contains("value1", result.StandardOutput, StringComparison.Ordinal);
        Assert.Contains("value2", result.StandardOutput, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunAsync_SetsWorkingDirectory()
    {
        // Arrange
        var expectedWorkingDirectory = Path.GetTempPath();
        var options = new BrunoRunOptions
        {
            BruExecutablePath = "dotnet",
            Target = "test.bru",
            WorkingDirectory = expectedWorkingDirectory,
        };
        var processFactory = Substitute.For<IProcessFactory>();
        ProcessStartInfo? capturedStartInfo = null;
        processFactory
            .Start(Arg.Any<ProcessStartInfo>())
            .Returns(callInfo =>
            {
                capturedStartInfo = callInfo.Arg<ProcessStartInfo>();
                // Return a real process that will complete quickly
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
    public async Task RunAsync_WhenProcessFactoryThrows_PropagatesException()
    {
        // Arrange
        var processFactory = Substitute.For<IProcessFactory>();
        processFactory
            .Start(Arg.Any<ProcessStartInfo>())
            .Returns(x =>
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
                // Return a real process that will complete quickly
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
            .Returns(callInfo =>
            {
                // Return a real process that will complete quickly
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
                // Return a real process that will complete quickly
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
                // Return a real process that will complete quickly
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
}
