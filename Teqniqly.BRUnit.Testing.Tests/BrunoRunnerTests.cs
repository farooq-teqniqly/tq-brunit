using System.Collections.Immutable;
using System.Diagnostics;
using NSubstitute;

namespace Teqniqly.BRUnit.Testing.Tests;

public class BrunoRunnerTests
{
    private static readonly TimeSpan TimeoutForTesting = TimeSpan.FromMilliseconds(100);

    [Fact]
    public void Constructor_WithNullProcessFactory_ThrowsArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() => new BrunoRunner(null!));

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
        var (processFactory, runner, capturedStartInfo) = SetupProcessFactoryMock();

        // Act
        await runner.RunAsync(options);

        // Assert
        processFactory.Received(1).Start(Arg.Any<ProcessStartInfo>());
        Assert.Single(capturedStartInfo);
        Assert.Equal(4, capturedStartInfo[0].ArgumentList.Count);
        Assert.Equal("run", capturedStartInfo[0].ArgumentList[0]);
        Assert.Equal("--env", capturedStartInfo[0].ArgumentList[1]);
        Assert.Equal("production", capturedStartInfo[0].ArgumentList[2]);
        Assert.Equal("test.bru", capturedStartInfo[0].ArgumentList[3]);
    }

    [Fact]
    public async Task RunAsync_BuildsCorrectArguments_WithoutEnvironmentName()
    {
        // Arrange
        var options = new BrunoRunOptions { BruExecutablePath = "dotnet", Target = "test.bru" };
        var (processFactory, runner, capturedStartInfo) = SetupProcessFactoryMock();

        // Act
        await runner.RunAsync(options);

        // Assert
        processFactory.Received(1).Start(Arg.Any<ProcessStartInfo>());
        Assert.Single(capturedStartInfo);
        Assert.Equal(2, capturedStartInfo[0].ArgumentList.Count);
        Assert.Equal("run", capturedStartInfo[0].ArgumentList[0]);
        Assert.Equal("test.bru", capturedStartInfo[0].ArgumentList[1]);
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
        var (processFactory, runner, capturedStartInfo) = SetupProcessFactoryMock();

        // Act
        await runner.RunAsync(options);

        // Assert
        processFactory.Received(1).Start(Arg.Any<ProcessStartInfo>());
        Assert.Single(capturedStartInfo);
        Assert.Equal(4, capturedStartInfo[0].ArgumentList.Count);
        Assert.Equal("run", capturedStartInfo[0].ArgumentList[0]);
        Assert.Equal("--env", capturedStartInfo[0].ArgumentList[1]);
        Assert.Equal("my environment", capturedStartInfo[0].ArgumentList[2]);
        Assert.Equal("test folder/test file.bru", capturedStartInfo[0].ArgumentList[3]);
    }

    [SkippableFact]
    public async Task RunAsync_OnUnix_DoesNotAddExeExtension()
    {
        Skip.If(OperatingSystem.IsWindows(), "Unix-specific test");

        // Arrange
        var options = new BrunoRunOptions { BruExecutablePath = "bru", Target = "test.bru" };
        var (processFactory, runner, capturedStartInfo) = SetupProcessFactoryMock();

        // Act
        await runner.RunAsync(options).ConfigureAwait(false);

        // Assert
        processFactory.Received(1).Start(Arg.Any<ProcessStartInfo>());
        Assert.Single(capturedStartInfo);
        Assert.Equal("bru", capturedStartInfo[0].FileName);
    }

    [SkippableFact]
    public async Task RunAsync_OnUnix_PreservesExplicitPath()
    {
        Skip.If(OperatingSystem.IsWindows(), "Unix-specific test");

        // Arrange
        var explicitPath = Path.Combine("usr", "bin", "bru");
        var options = new BrunoRunOptions { BruExecutablePath = explicitPath, Target = "test.bru" };
        var (processFactory, runner, capturedStartInfo) = SetupProcessFactoryMock();

        // Act
        await runner.RunAsync(options).ConfigureAwait(false);

        // Assert
        processFactory.Received(1).Start(Arg.Any<ProcessStartInfo>());
        Assert.Single(capturedStartInfo);
        Assert.Equal(explicitPath, capturedStartInfo[0].FileName);
    }

    [SkippableFact]
    public async Task RunAsync_OnWindows_AddsExeExtension_WhenNotPresent()
    {
        Skip.If(!OperatingSystem.IsWindows(), "Windows-specific test");

        // Arrange
        var options = new BrunoRunOptions { BruExecutablePath = "bru", Target = "test.bru" };
        var (processFactory, runner, capturedStartInfo) = SetupProcessFactoryMock();

        // Act
        await runner.RunAsync(options).ConfigureAwait(false);

        // Assert
        processFactory.Received(1).Start(Arg.Any<ProcessStartInfo>());
        Assert.Single(capturedStartInfo);
        // On Windows, npm-installed packages use .cmd files. The resolver checks for .cmd first,
        // then falls back to .exe. Accept either behavior.
        var fileName = capturedStartInfo[0].FileName;
        var actualFileName = Path.GetFileName(fileName);
        Assert.True(
            fileName == "bru.exe"
                || string.Equals(actualFileName, "bru.cmd", StringComparison.OrdinalIgnoreCase),
            $"Expected 'bru.exe' or path with filename 'bru.cmd', but got '{fileName}'"
        );
    }

    [SkippableFact]
    public async Task RunAsync_OnWindows_PreservesExeExtension_CaseInsensitive()
    {
        Skip.If(!OperatingSystem.IsWindows(), "Windows-specific test");

        // Arrange
        var options = new BrunoRunOptions { BruExecutablePath = "bru.EXE", Target = "test.bru" };
        var (processFactory, runner, capturedStartInfo) = SetupProcessFactoryMock();

        // Act
        await runner.RunAsync(options).ConfigureAwait(false);

        // Assert
        processFactory.Received(1).Start(Arg.Any<ProcessStartInfo>());
        Assert.Single(capturedStartInfo);
        Assert.Equal("bru.EXE", capturedStartInfo[0].FileName);
    }

    [SkippableFact]
    public async Task RunAsync_OnWindows_PreservesExeExtension_WhenAlreadyPresent()
    {
        Skip.If(!OperatingSystem.IsWindows(), "Windows-specific test");

        // Arrange
        var options = new BrunoRunOptions { BruExecutablePath = "bru.exe", Target = "test.bru" };
        var (processFactory, runner, capturedStartInfo) = SetupProcessFactoryMock();

        // Act
        await runner.RunAsync(options).ConfigureAwait(false);

        // Assert
        processFactory.Received(1).Start(Arg.Any<ProcessStartInfo>());
        Assert.Single(capturedStartInfo);
        Assert.Equal("bru.exe", capturedStartInfo[0].FileName);
    }

    [SkippableFact]
    public async Task RunAsync_OnWindows_PreservesExplicitPath_WithSeparators()
    {
        Skip.If(!OperatingSystem.IsWindows(), "Windows-specific test");

        // Arrange
        var explicitPath = Path.Combine("Program Files", "bru", "bru");
        var options = new BrunoRunOptions { BruExecutablePath = explicitPath, Target = "test.bru" };
        var (processFactory, runner, capturedStartInfo) = SetupProcessFactoryMock();

        // Act
        await runner.RunAsync(options).ConfigureAwait(false);

        // Assert
        processFactory.Received(1).Start(Arg.Any<ProcessStartInfo>());
        Assert.Single(capturedStartInfo);
        Assert.Equal(explicitPath, capturedStartInfo[0].FileName);
    }

    [SkippableFact]
    public async Task RunAsync_OnWindows_PreservesRootedPath()
    {
        Skip.If(!OperatingSystem.IsWindows(), "Windows-specific test");

        // Arrange
        var rootedPath = Path.Combine("C:", "Program Files", "bru", "bru.exe");
        var options = new BrunoRunOptions { BruExecutablePath = rootedPath, Target = "test.bru" };
        var (processFactory, runner, capturedStartInfo) = SetupProcessFactoryMock();

        // Act
        await runner.RunAsync(options).ConfigureAwait(false);

        // Assert
        processFactory.Received(1).Start(Arg.Any<ProcessStartInfo>());
        Assert.Single(capturedStartInfo);
        Assert.Equal(rootedPath, capturedStartInfo[0].FileName);
    }

    [Fact]
    public async Task RunAsync_WhenBrunoFails_ReturnsNonZeroExitCode()
    {
        // Arrange
        // Use dotnet with an invalid command to produce a non-zero exit code
        var options = new BrunoRunOptions
        {
            BruExecutablePath = "dotnet",
            Target = "invalidcommand",
        };
        var runner = new BrunoRunner(new ProcessFactory());

        // Act
        var result = await runner.RunAsync(options);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotEqual(0, result.ExitCode);
    }

    [SkippableFact]
    public async Task RunAsync_WhenExecutionExceedsTimeout_ThrowsTimeoutException()
    {
        // Arrange
        var (hangingProcess, options, processFactory) = SetupTimeoutTest(CreateHangingProcess);
        var runner = new BrunoRunner(processFactory);

        try
        {
            // Act & Assert
            var exception = await Assert
                .ThrowsAsync<TimeoutException>(() => runner.RunAsync(options))
                .ConfigureAwait(false);
            Assert.Contains("timeout", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("0.1", exception.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            CleanupHangingProcess(hangingProcess);
        }
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

    [SkippableFact]
    public async Task RunAsync_WhenTimeoutOccurs_KillsProcess()
    {
        // Arrange
        var (hangingProcess, options, processFactory) = SetupTimeoutTest(CreateHangingProcess);
        var runner = new BrunoRunner(processFactory);

        try
        {
            var startTime = DateTime.UtcNow;

            // Act
            try
            {
                await runner.RunAsync(options).ConfigureAwait(false);
                Assert.Fail("Expected TimeoutException was not thrown.");
            }
            catch (TimeoutException)
            {
                // Expected
            }

            var elapsed = DateTime.UtcNow - startTime;

            // Assert
            Assert.True(elapsed.TotalSeconds < 2, "Process should have been killed quickly");

            try
            {
                hangingProcess?.Refresh();
                Assert.True(hangingProcess?.HasExited ?? false, "Process should have been killed");
            }
            catch (InvalidOperationException)
            {
                // Process already exited/killed - expected
            }
        }
        finally
        {
            CleanupHangingProcess(hangingProcess);
        }
    }

    [SkippableFact]
    public async Task RunAsync_WhenTimeoutOccurs_ThrowsTimeoutException_WithOutputGeneratingProcess()
    {
        // Arrange
        var (hangingProcess, options, processFactory) = SetupTimeoutTest(
            CreateHangingProcessWithOutput
        );
        var runner = new BrunoRunner(processFactory);

        try
        {
            // Act & Assert
            var exception = await Assert
                .ThrowsAsync<TimeoutException>(() => runner.RunAsync(options))
                .ConfigureAwait(false);
            Assert.NotNull(exception);
            // Note: We can't easily verify partial output was captured without exposing internal state,
            // but the fact that the exception was thrown means the timeout logic executed
        }
        finally
        {
            CleanupHangingProcess(hangingProcess);
        }
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
        var (processFactory, runner, capturedStartInfo) = SetupProcessFactoryMock();

        // Act
        await runner.RunAsync(options);

        // Assert
        processFactory.Received(1).Start(Arg.Any<ProcessStartInfo>());
        Assert.Single(capturedStartInfo);
        Assert.Equal(expectedWorkingDirectory, capturedStartInfo[0].WorkingDirectory);
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
                var processFactory = new ProcessFactory();
                return processFactory.Start(startInfo);
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

    [Theory]
    [InlineData("TEST_VAR_1", "value1", "TEST_VAR_2", "value2")]
    [InlineData("CUSTOM_VAR", "custom_value", "ANOTHER_VAR", "another_value")]
    public async Task RunAsync_WithEnvironmentVariables_PassesToProcessStartInfo(
        string var1Name,
        string var1Value,
        string var2Name,
        string var2Value
    )
    {
        // Arrange
        var envVars = ImmutableDictionary<string, string?>
            .Empty.WithComparers(StringComparer.OrdinalIgnoreCase)
            .Add(var1Name, var1Value)
            .Add(var2Name, var2Value);

        var options = new BrunoRunOptions
        {
            BruExecutablePath = "dotnet",
            Target = "--version",
            EnvironmentVariables = envVars,
        };
        var (processFactory, runner, capturedStartInfo) = SetupProcessFactoryMock();

        // Act
        await runner.RunAsync(options);

        // Assert
        processFactory.Received(1).Start(Arg.Any<ProcessStartInfo>());
        Assert.Single(capturedStartInfo);
        Assert.Equal(var1Value, capturedStartInfo[0].Environment[var1Name]);
        Assert.Equal(var2Value, capturedStartInfo[0].Environment[var2Name]);
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
        var (processFactory, runner, capturedStartInfo) = SetupProcessFactoryMock();

        // Act
        await runner.RunAsync(options);

        // Assert
        processFactory.Received(1).Start(Arg.Any<ProcessStartInfo>());
        Assert.Single(capturedStartInfo);
        Assert.Equal(string.Empty, capturedStartInfo[0].Environment["NULL_VAR"]);
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
    public async Task RunAsync_WithRealBrunoCli_AndEnvironmentName_ExecutesSuccessfully()
    {
        // Arrange
        var bruPath = FindBrunoCliPath();
        Skip.If(string.IsNullOrEmpty(bruPath), "Bruno CLI not available");

        // Use bru run --env test-env -h to verify environment flag is accepted
        // Even if the environment doesn't exist, Bruno CLI should accept the flag syntax
        // and either show help or an error about the environment, but the command structure should be valid
        var options = new BrunoRunOptions
        {
            BruExecutablePath = bruPath,
            Target = "-h",
            EnvironmentName = "test-env",
        };
        var runner = new BrunoRunner(new ProcessFactory());

        // Act
        var result = await runner.RunAsync(options).ConfigureAwait(false);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.ExitCode);
        Assert.NotEmpty(result.StandardOutput);
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

    private static void CleanupHangingProcess(Process? hangingProcess)
    {
        try
        {
            if (hangingProcess?.HasExited == false)
            {
                hangingProcess.Kill(entireProcessTree: true);
            }
        }
#pragma warning disable CA1031 // Do not catch general exception types
        catch
#pragma warning restore CA1031
        {
            // Ignore cleanup errors
        }

        hangingProcess?.Dispose();
    }

    private static Process CreateHangingProcess()
    {
        var processFactory = new ProcessFactory();
        if (OperatingSystem.IsWindows())
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "ping",
                Arguments = "-n 11 127.0.0.1",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            return processFactory.Start(startInfo);
        }
        else
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "sleep",
                Arguments = "10",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            return processFactory.Start(startInfo);
        }
    }

    private static Process CreateHangingProcessWithOutput()
    {
        var processFactory = new ProcessFactory();
        if (OperatingSystem.IsWindows())
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "powershell",
                Arguments = "-Command \"Write-Output 'test-output'; Start-Sleep -Seconds 10\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            return processFactory.Start(startInfo);
        }
        else
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "sh",
                Arguments = "-c \"echo test-output && sleep 10\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            return processFactory.Start(startInfo);
        }
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

    private static (
        IProcessFactory processFactory,
        BrunoRunner runner,
        List<ProcessStartInfo> capturedStartInfo
    ) SetupProcessFactoryMock()
    {
        var processFactory = Substitute.For<IProcessFactory>();
        List<ProcessStartInfo> capturedStartInfo = [];
        processFactory
            .Start(Arg.Do<ProcessStartInfo>(x => capturedStartInfo.Add(x)))
            .Returns(callInfo =>
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };
                var realProcessFactory = new ProcessFactory();
                return realProcessFactory.Start(startInfo);
            });
        var runner = new BrunoRunner(processFactory);
        return (processFactory, runner, capturedStartInfo);
    }

    private static (
        Process hangingProcess,
        BrunoRunOptions options,
        IProcessFactory processFactory
    ) SetupTimeoutTest(Func<Process> createHangingProcess)
    {
        var processFactory = Substitute.For<IProcessFactory>();
        var hangingProcess = createHangingProcess();
        processFactory.Start(Arg.Any<ProcessStartInfo>()).Returns(hangingProcess);

        var options = new BrunoRunOptions
        {
            BruExecutablePath = "bru",
            Target = "test.bru",
            Timeout = TimeoutForTesting,
        };

        return (hangingProcess, options, processFactory);
    }

    private static bool TryRunBrunoVersion(string bruPath)
    {
        try
        {
            var processFactory = new ProcessFactory();
            var startInfo = new ProcessStartInfo
            {
                FileName = bruPath,
                Arguments = "--version",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            using var process = processFactory.Start(startInfo);

            process.WaitForExit();
            return process.ExitCode == 0
                || !string.IsNullOrEmpty(process.StandardOutput.ReadToEnd());
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
